# Contrato de tokens

Propuesta de la pareja de Autorización para el issue #45. Queda abierta a los comentarios de las demás parejas en el pull request y entra en `dev` cuando la aprueben.

## Qué resuelve

Define cómo obtiene un token cada parte del sistema, qué datos trae el token y cómo lo valida el servicio que lo recibe. Cubre el paso 2 del enunciado, en el que el usuario obtiene un token antes de reservar, y el punto 3.e, en el que los microservicios se autentican entre sí con el servidor de autorización.

Completa la sección "Autorización" del contrato entre Availability y Booking (`contracts/availability-booking.md`).

## Servidor de autorización

El servidor de autorización es Keycloak 26.0.8. Corre en Docker, con su propia base de datos, y su configuración está en `src/Auth/keycloak/`.

| Dato | Valor |
|---|---|
| Dirección | `http://localhost:8080`, solo desde la propia PC |
| Realm | `reserva-asientos` |
| Emisor (`Authority`) | `http://localhost:8080/realms/reserva-asientos` |
| Endpoint de tokens | `http://localhost:8080/realms/reserva-asientos/protocol/openid-connect/token` |
| Claves públicas | `http://localhost:8080/realms/reserva-asientos/protocol/openid-connect/certs` |

En desarrollo Keycloak corre en HTTP, así que los servicios configuran `RequireHttpsMetadata = false`.

## Quién pide un token y para qué

| Llamada | Quién pide el token | Cómo lo pide | Audiencia (`aud`) |
|---|---|---|---|
| Usuario → Availability (login) | Availability, en nombre del usuario | OpenID Connect con *authorization code*. El usuario entra con usuario y contraseña de Keycloak o con GitHub | — |
| Availability → Booking (REST) | Client `availability-web` | *Client credentials* con `scope=booking-api` | `booking-api` |
| Booking → Payment (gRPC) | Client `booking-api` | *Client credentials* con `scope=payment-api` | `payment-api` |

El usuario inicia sesión en la página de login de Keycloak, y Availability guarda la sesión en una cookie. El token del usuario no viaja a Booking ni a Payment: las llamadas entre servicios usan siempre el token de servicio del que llama.

La audiencia de cada API se agrega solo cuando el token se pide con su `scope`. Por eso el token de login del usuario no trae `booking-api` en `aud`, y si llegara a Booking, Booking lo rechazaría con `401`.

El client `payment-api` no pide tokens. Existe solo para que Payment tenga una audiencia propia.

## Proveedor de identidad externo

Keycloak ofrece iniciar sesión con GitHub, que es el proveedor de identidad externo del diagrama. Keycloak hace de intermediario (*identity brokering*):

1. En la página de login de Keycloak, el usuario toca "GitHub".
2. GitHub le pide iniciar sesión y autorizar la aplicación. Si ya tiene la sesión de GitHub abierta y autorizó antes, no le pide nada.
3. GitHub lo devuelve a Keycloak. La primera vez, Keycloak le crea un usuario y le pide confirmar sus datos.
4. Keycloak emite su propio token, igual que para un usuario local.

Para Availability, Booking y Payment no cambia nada: el token siempre lo emite Keycloak, con el mismo emisor (`iss`). Los servicios nunca reciben un token de GitHub. En `preferred_username` viene el usuario de GitHub. El usuario no tiene contraseña en Keycloak.

Cada integrante que levanta Keycloak registra su propia GitHub OAuth App (*Settings → Developer settings → OAuth Apps*) con la *callback URL* `http://localhost:8080/realms/reserva-asientos/broker/github/endpoint`, y pone el Client ID y el secret en `GITHUB_CLIENT_ID` y `GITHUB_CLIENT_SECRET` del `.env`. Para iniciar sesión, en cambio, alcanza con cualquier cuenta de GitHub. Si las variables quedan en `cambiar`, Keycloak arranca igual y solo falla el botón de GitHub.

## Cómo se pide un token de servicio

El servicio que llama hace un `POST` al endpoint de tokens con su client id, su client secret y el `scope` de la API a la que va a llamar.

```http
POST /realms/reserva-asientos/protocol/openid-connect/token
Host: localhost:8080
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=availability-web&client_secret=<secret>&scope=booking-api
```

Respuesta (resumida):

```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIs...",
  "expires_in": 300,
  "token_type": "Bearer"
}
```

- Sin `scope`, el token no trae la audiencia de la API y la API lo rechaza con `401`. Cada client solo puede pedir el scope que tiene asignado: si pide otro, Keycloak responde `400 invalid_scope`.
- El token dura 5 minutos (`expires_in` en segundos).
- El servicio guarda el token en memoria y lo reusa hasta 30 segundos antes de que venza. Recién ahí pide uno nuevo. Así no pide un token por cada llamada, y nunca manda uno a punto de vencer.
- El client secret no va en `appsettings.json` ni en el repo. Cada integrante lo guarda con `dotnet user-secrets set "Keycloak:ClientSecret" "<secret>"`, con el mismo valor que tiene en el `.env` de Keycloak.

## Cómo viaja el token

- **REST:** en el header `Authorization`.

  ```http
  Authorization: Bearer <token>
  ```

- **gRPC:** en la metadata `authorization` de la llamada, con el mismo valor `Bearer <token>`.

## Qué trae el token

El token es un JWT firmado con RS256. Los servicios usan estos campos:

| Campo | Ejemplo | Significado |
|---|---|---|
| `iss` | `http://localhost:8080/realms/reserva-asientos` | Quién emitió el token. |
| `aud` | `["booking-api", "account"]` | Para qué servicio es el token. |
| `azp` | `availability-web` | Qué client lo pidió. Sirve para los logs de auditoría. |
| `exp` | `1761700000` | Cuándo vence, en segundos desde 1970 (UTC). |
| `sub` | `4a6cbdb7-...` | Identificador del usuario o de la cuenta de servicio. |
| `preferred_username` | `service-account-availability-web` | Nombre legible. En un token de usuario es, por ejemplo, `usuario1`. |

Keycloak agrega `account` en `aud` por defecto. Los servicios lo ignoran.

## Cómo valida el token el servicio que lo recibe

Booking y Payment validan cada token que llega con `AddValidacionKeycloak()`, del proyecto compartido `Shared.Auth`. La validación comprueba cuatro cosas:

1. **Firma.** El token está firmado con la clave privada de Keycloak. El servicio descarga la clave pública desde el endpoint de claves públicas, a partir del `Authority`. Ningún servicio tiene una clave escrita en su configuración.
2. **Emisor.** `iss` es igual al `Authority`.
3. **Audiencia.** `aud` contiene el client id del servicio: `booking-api` en Booking y `payment-api` en Payment. Así, un token pedido para otro servicio no sirve.
4. **Vencimiento.** `exp` todavía no llegó, con una tolerancia de reloj (`ClockSkew`) de 30 segundos. ASP.NET Core usa 5 minutos por defecto, lo que con tokens de 5 minutos duplicaría su vida.

Cada servicio configura la sección `Keycloak` de su `appsettings.json`. Ejemplo para Booking:

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/reserva-asientos",
  "Audience": "booking-api",
  "ClientId": "booking-api"
}
```

## Respuestas cuando el token no sirve

| Caso | REST (Booking) | gRPC (Payment) |
|---|---|---|
| Falta el token | `401 Unauthorized` | `StatusCode.Unauthenticated` |
| La firma no es válida o el token fue modificado | `401 Unauthorized` | `StatusCode.Unauthenticated` |
| El token venció | `401 Unauthorized` | `StatusCode.Unauthenticated` |
| El token es para otro servicio (`aud` incorrecta) | `401 Unauthorized` | `StatusCode.Unauthenticated` |
| El token lo emitió otro servidor (`iss` incorrecto) | `401 Unauthorized` | `StatusCode.Unauthenticated` |

Esta versión no usa `403 Forbidden` (en gRPC, `StatusCode.PermissionDenied`). Ningún servicio distingue permisos entre clients: un token válido con la audiencia correcta alcanza para usar todos los endpoints del servicio. Si más adelante algún endpoint necesita permisos propios, se agregan roles en Keycloak y `403` para el token válido sin el rol.

El cuerpo de la respuesta `401` no forma parte del contrato. El servicio que llama decide solo a partir del código de estado.

## Datos de prueba

| Dato | Valor |
|---|---|
| Usuario de prueba | `usuario1` / `usuario1` |
| Usuario de GitHub | Cualquier cuenta de GitHub, con el botón "GitHub" del login. Requiere la OAuth App en el `.env` |
| Panel de Keycloak | `http://localhost:8080`, con el usuario y la clave de `KEYCLOAK_ADMIN_*` del `.env` |
| Client secrets | Cada integrante genera los suyos en el `.env` de `src/Auth/keycloak/` y usa el mismo valor en los user-secrets del servicio |

## Temas abiertos

- Si en la demo los servicios corren dentro de contenedores, `localhost:8080` no apunta a Keycloak desde adentro de un contenedor. Habría que acordar otra dirección y cuidar que el emisor del token siga coincidiendo con el `Authority`.
- Los logs de auditoría de Autorización (token aceptado, token rechazado y token de servicio pedido) usan el formato que definan Facundo y Gianluca en `Shared.Observability`.
- Las parejas acuerdan si Booking necesita saber qué usuario hizo la reserva. Hoy el token que llega a Booking es el de Availability, no el del usuario, así que ese dato tendría que viajar en el mensaje del broker.
