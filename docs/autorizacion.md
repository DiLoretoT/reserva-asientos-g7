# Autorización con Keycloak: guía para los servicios

La validación de tokens y los tokens de servicio entre microservicios viven en la librería compartida `Shared.Auth`. Cada servicio que la usa agrega unas pocas líneas: **no hay que escribir configuración de JWT a mano**.

`Shared.Auth` ya está en `dev`. El login de usuarios ya está implementado en el propio Availability (issue #6, PR #74), y el token que Availability manda a Booking lo pide su propio código (`TokenServicio`), así que Availability **no usa `Shared.Auth`**.

## 1. Qué usa cada servicio

| Servicio | Qué hace con la autorización | De `Shared.Auth` usa | Necesita `ClientSecret` |
|---|---|---|---|
| **Booking** (REST) | Valida los tokens que le llegan y pide token para llamar a Payment | `AddValidacionKeycloak()` y `AddTokenDeServicio("payment-api")` | Sí |
| **Payment** (gRPC) | Solo valida los tokens que le llegan | `AddValidacionKeycloak()` | **No** |
| **Availability** (MVC) | Login de usuarios y token hacia Booking, con código propio (#74) | Nada | Sí |

El parámetro de `AddTokenDeServicio` es la **audiencia**: el servicio al que se llama (el destino), no el propio. Keycloak solo pone esa audiencia en el token si se la pide con `scope=<audiencia>`, y eso lo hace `Shared.Auth` por vos.

## 2. Datos de Keycloak

| Dato | Valor |
|---|---|
| URL | `http://localhost:8080` |
| Realm | `reserva-asientos` |
| Authority | `http://localhost:8080/realms/reserva-asientos` |
| Clients | `availability-web`, `booking-api`, `payment-api` |
| Usuario de prueba | `usuario1` / `usuario1` |

Los **client secrets** (`AVAILABILITY_WEB_CLIENT_SECRET` y `BOOKING_API_CLIENT_SECRET`) los genera cada integrante en su propio `.env` (ver 3.1) y usa **el mismo valor** en sus `user-secrets` (ver 3.5). Payment no tiene secret. Nunca van en el repo ni en el `appsettings.json`.

El contrato completo de tokens está en `contracts/tokens.md`.

## 3. Pasos comunes

### 3.1 Levantar Keycloak

Desde la carpeta `src/Auth/keycloak/`:

1. Copiá el archivo de ejemplo y completá tus secrets:

   ```
   copy .env.example .env
   ```

   Abrí `.env` y completá:

   | Variable | Qué poner |
   |---|---|
   | `KEYCLOAK_ADMIN_USER` y `KEYCLOAK_ADMIN_PASSWORD` | Ya vienen con `admin` / `admin`. No hace falta tocarlos |
   | `AVAILABILITY_WEB_CLIENT_SECRET` y `BOOKING_API_CLIENT_SECRET` | Un valor que generes vos. Sirve cualquiera, siempre que sea **el mismo** que guardás en los `user-secrets` de ese servicio (ver 3.5). Por ejemplo, con `openssl rand -hex 24` o, en PowerShell, `[guid]::NewGuid().ToString("N")` |
   | `GITHUB_CLIENT_ID` y `GITHUB_CLIENT_SECRET` | Opcionales (ver 3.6). Si quedan en `cambiar`, Keycloak arranca igual y solo falla el login con GitHub |

   **El `.env` no se sube al repo.**

2. Levantá Keycloak:

   ```
   docker compose down
   docker compose up -d
   ```

   Hacé siempre `down` antes de `up -d`: si el contenedor ya existía, `up` solo no toma el realm nuevo.

Esperá uno o dos minutos y comprobá que carga `http://localhost:8080`.

### 3.2 Traer `Shared.Auth` a tu rama

`Shared.Auth` ya está en `dev`:

```
git fetch origin
git merge origin/dev
```

### 3.3 Referenciar el proyecto (Booking y Payment)

Desde la raíz del repo.

Booking: la referencia a `Shared.Auth` **ya está en `dev`** (`feat/auth-booking`, #84). No hay que agregarla: con el `git merge origin/dev` de 3.2 ya la tenés.

Payment:
```
dotnet add src/Payment/Payment.Grpc/Payment.Grpc.csproj reference src/Shared/Shared.Auth/Shared.Auth.csproj
```

Después, en el `Program.cs`: `using Shared.Auth;` (en Booking ya está en `dev`).

Availability no necesita esta referencia.

### 3.4 Orden del pipeline (Booking y Payment)

`UseAuthentication()` va **antes** de `UseAuthorization()`, y los dos antes de mapear endpoints:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### 3.5 Guardar el secret (Booking y Availability)

En la carpeta del proyecto que tiene el `Program.cs` (`src/Booking/Booking.Api` para Booking, `src/Availability/Availability.Web` para Availability), con **el mismo valor que pusiste en tu `.env`**: `BOOKING_API_CLIENT_SECRET` para Booking y `AVAILABILITY_WEB_CLIENT_SECRET` para Availability:

```
dotnet user-secrets init
dotnet user-secrets set "Keycloak:ClientSecret" "EL_SECRET_DE_TU_CLIENT"
dotnet user-secrets list
```

En Availability ese mismo secret lo usan el login y el token hacia Booking: no hay que cargar dos.

Los user-secrets se cargan solo en `Development` y viven en tu PC, no en el repo. Cada integrante tiene que hacerlo en su máquina.

### 3.6 Login con GitHub (opcional)

El profe pidió que el proveedor de identidad externo funcione de verdad, y elegimos GitHub. Para desarrollar es opcional: sin esto, todo lo demás funciona igual con `usuario1`. **La PC de la demo sí necesita su OAuth App.**

1. En GitHub: **Settings → Developer settings → OAuth Apps → New OAuth App**.
2. Completá:
   - Homepage URL: `http://localhost:8080` (GitHub exige una; esta sirve).
   - Authorization callback URL: `http://localhost:8080/realms/reserva-asientos/broker/github/endpoint`
3. Creá la app, copiá el **Client ID** y generá un **Client secret**.
4. Pegalos en tu `.env`:
   ```
   GITHUB_CLIENT_ID=...
   GITHUB_CLIENT_SECRET=...
   ```
5. Reiniciá Keycloak: `docker compose down` y después `docker compose up -d`.

Cada integrante registra **su propia** OAuth App. El secret de GitHub tampoco se sube al repo. Si dejás `GITHUB_CLIENT_ID` y `GITHUB_CLIENT_SECRET` en `cambiar`, Keycloak arranca igual y solo falla el login con GitHub.

## 4. Booking (API REST)

Recibe llamadas de Availability y llama a Payment.

**`appsettings.json`:** la sección `Keycloak` **ya está en `dev`**, no hay que agregarla. Solo verificá que coincida con esta:

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/reserva-asientos",
  "Audience": "booking-api",
  "ClientId": "booking-api"
}
```

**Qué ya está en `dev` y qué falta:** la referencia a `Shared.Auth` y la validación de Booking (`using Shared.Auth;`, `AddValidacionKeycloak()`, `UseAuthentication()` y `UseAuthorization()`) **ya están en `dev`** (`feat/auth-booking`, #84). No las agregues de nuevo. Lo que falta es el cliente gRPC hacia Payment con el token de servicio.

**Paquetes y proto (cliente gRPC):** `Grpc.Net.ClientFactory` solo no alcanza. Booking no tiene el `.proto`, y sin él `PaymentGrpc.PaymentGrpcClient` no se genera. Hay que agregar tres paquetes:

```
dotnet add src/Booking/Booking.Api/Booking.Api.csproj package Grpc.Net.ClientFactory
dotnet add src/Booking/Booking.Api/Booking.Api.csproj package Grpc.Tools
dotnet add src/Booking/Booking.Api/Booking.Api.csproj package Google.Protobuf
```

y el proto como cliente, en `src/Booking/Booking.Api/Booking.Api.csproj`:

```xml
<ItemGroup>
  <Protobuf Include="..\..\Payment\Payment.Grpc\Protos\Payment.proto" GrpcServices="Client" />
</ItemGroup>
```

**`Program.cs`:** las líneas marcadas `ya está en dev` no hay que agregarlas. Las marcadas `FALTA` son el cliente gRPC; el resto es lo que ya existe:

```csharp
using Booking.Api;
using Booking.Business;
using Booking.Data;
using Microsoft.EntityFrameworkCore;
using Payment.Grpc;                                  // FALTA: para PaymentGrpc.PaymentGrpcClient
using Shared.Auth;                                   // ya está en dev

var builder = WebApplication.CreateBuilder(args);

// ... lo que ya hay: OpenApi, controllers, BookingOptions, DbContext, etc.

builder.AddValidacionKeycloak();                     // ya está en dev: valida los tokens que le llegan

// FALTA: cliente gRPC hacia Payment, con el token de servicio en cada llamada
builder.Services.AddGrpcClient<PaymentGrpc.PaymentGrpcClient>(o => o.Address = new Uri("http://localhost:5043"))
    .AddTokenDeServicio("payment-api");

var app = builder.Build();

// ... lo que ya hay: migraciones y OpenAPI en desarrollo

app.UseHttpsRedirection();
app.UseAuthentication();                             // ya está en dev
app.UseAuthorization();                              // ya está en dev
app.MapControllers();

app.Run();
```

`AddTokenDeServicio("payment-api")` tiene que ir encadenado al cliente gRPC. Sin esa línea Booking no manda token a Payment y, cuando Payment active su autenticación, recibe `Unauthenticated`.

> **Pendiente de probar:** el cliente gRPC (paquetes, proto, `using Payment.Grpc;` y la dirección `http://localhost:5043`) todavía no se probó. Que lo confirme Booking cuando haga la llamada.

**Controllers:** `[Authorize]` sobre los controllers o acciones que deben estar protegidos (`using Microsoft.AspNetCore.Authorization;`).

**Secret:** el de `booking-api` (ver 3.5).

## 5. Payment (API gRPC)

Solo recibe llamadas. **No lleva secret ni `AddTokenDeServicio`.**

> **Activalo recién cuando Booking le mande el token por gRPC.** Si se activa antes, Booking recibe `Unauthenticated` en cada llamada.
>
> Estado al escribir esta guía: la referencia y la validación de Booking ya están en `dev` (`feat/auth-booking`, #84), pero la llamada gRPC de Booking a Payment todavía falta.

**`appsettings.json`:** la sección `Keycloak` **ya está en `dev`**, no hay que agregarla. Solo verificá que coincida con esta:

```json
"Keycloak": {
  "Authority": "http://localhost:8080/realms/reserva-asientos",
  "Audience": "payment-api",
  "ClientId": "payment-api"
}
```

**Qué hay que hacer:** en `dev`, el `Program.cs` de Payment tiene comentadas las tres líneas de autenticación, y `PaymentGrpcService.cs` tiene comentado el `[Authorize]` (el `using Microsoft.AspNetCore.Authorization;` ya está). Cuando Booking ya mande el token:

1. Agregar la referencia a `Shared.Auth` (ver 3.3).
2. En `Program.cs`: agregar `using Shared.Auth;` y descomentar `builder.AddValidacionKeycloak();`, `app.UseAuthentication();` y `app.UseAuthorization();`.
3. En `PaymentGrpcService.cs`: descomentar `[Authorize]`.

Queda así:

```csharp
// Program.cs
using Shared.Auth;
// ...
builder.Services.AddScoped<PaymentService>();
builder.AddValidacionKeycloak();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<PaymentGrpcService>();
```

```csharp
// PaymentGrpcService.cs
[Authorize]
public class PaymentGrpcService(PaymentService paymentService) : PaymentGrpc.PaymentGrpcBase
```

**Ojo con `/payment-test`:** el `[Authorize]` de la clase protege solo el servicio gRPC. El endpoint HTTP de prueba `/payment-test` sigue abierto. Decidí si lo dejás así (solo para pruebas) o le agregás `.RequireAuthorization()`.

## 6. Availability (sitio MVC)

**No hay que cambiar código.** Availability ya tiene el login con Keycloak, el `[Authorize]` en la solicitud de reserva y manda el token a Booking con su propio `TokenServicio` (#74). La consulta de asientos sigue siendo pública.

Lo único que necesita cada integrante es cargar su secret y probar.

### Cargar el secret

En la carpeta `src/Availability/Availability.Web` (el proyecto que tiene el `Program.cs`), con el valor de `AVAILABILITY_WEB_CLIENT_SECRET` de tu `.env`:

```
dotnet user-secrets init
dotnet user-secrets set "Keycloak:ClientSecret" "EL_SECRET_DE_AVAILABILITY_WEB"
dotnet user-secrets list
```

### Probar

1. Levantá Keycloak (3.1) y las bases de datos de Booking y de Availability. Para la de Booking, mirá `docs/booking-postgresql.md`.
2. Levantá Booking y Availability. Booking se corre con el perfil `http` (desde la raíz del repo):
   ```
   cd src/Booking/Booking.Api
   dotnet run --launch-profile http
   ```
   Availability se corre con el perfil `https` (desde la raíz del repo):
   ```
   cd src/Availability/Availability.Web
   dotnet run --launch-profile https
   ```
   y se entra por `https://localhost:7113`. Por `http`, el login puede fallar con `Correlation failed`.
3. Consultá los asientos sin iniciar sesión: tiene que funcionar.
4. Pedí una reserva: te redirige al login de Keycloak. Entrá con `usuario1` / `usuario1` (o con GitHub, si hiciste 3.6).
5. En los logs de Booking, si ya tiene `AddValidacionKeycloak()` activo, tiene que aparecer `AUTH token aceptado`.

Si algo da 401, mirá la sección 9.

## 7. Cómo probar los tokens

Con Keycloak arriba y el servicio corriendo. Ejemplo con la extensión **REST Client** de VS Code. No subas ese archivo `.http`: tiene secrets.

```
@kc = http://localhost:8080/realms/reserva-asientos/protocol/openid-connect/token

### Token válido para Booking (client availability-web, scope booking-api)
POST {{kc}}
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=availability-web&client_secret=TU_SECRET_AVAILABILITY&scope=booking-api

### Token pensado para Payment (client booking-api, scope payment-api): se usa contra Booking para probar el 401
POST {{kc}}
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id=booking-api&client_secret=TU_SECRET_BOOKING&scope=payment-api

### Sin token
GET http://localhost:PUERTO_BOOKING/RUTA_PROTEGIDA

### Con token
GET http://localhost:PUERTO_BOOKING/RUTA_PROTEGIDA
Authorization: Bearer PEGAR_ACCESS_TOKEN
```

| Prueba | Cómo | Resultado esperado |
|---|---|---|
| Sin token | Endpoint protegido de Booking, sin header | 401 y log `Motivo=sin token` |
| Token válido | Token de `availability-web` con `scope=booking-api` | 200 y log `token aceptado` |
| Token de otro servicio | Token de `booking-api` con `scope=payment-api`, usado contra Booking | 401 y log `Motivo=audiencia incorrecta` |
| Token modificado | Cambiá una letra del final del token | 401 y log `Motivo=firma inválida` |
| Token vencido | Pedí un token y esperá más de 5 min 30 s (dura 5 minutos, más 30 segundos de tolerancia) | 401 y log `Motivo=token vencido`. **Pendiente de probar** |

**Ojo con la prueba de "otro servicio":** pedir un token con `client_id=availability-web` y `scope=payment-api` no sirve. Keycloak responde `invalid_scope` y no entrega ningún token. Para probar el 401 hay que pedirlo con `client_id=booking-api` y `scope=payment-api`, y usar ese token contra Booking.

El token dura 5 minutos. Si da 401 con un token que antes andaba, pedí uno nuevo.

**Payment (gRPC):** se prueba a través de Booking, cuando esté la llamada gRPC (ver 5). Hacé una reserva por el flujo normal: en Booking tiene que aparecer el pedido del token de servicio y en Payment `token aceptado`.

## 8. Logs de auditoría

Salen solos de `Shared.Auth`; no hay que programar nada. Todos empiezan con `AUTH` y **nunca se loguea el token ni el secret**.

| Línea | Cuándo |
|---|---|
| `AUTH token aceptado. Quien=... Endpoint=GET /ruta` | La API aceptó el token |
| `AUTH token rechazado. Motivo=... Endpoint=...` | Token vencido, firma inválida, audiencia incorrecta, emisor incorrecto o inválido |
| `AUTH solicitud rechazada. Motivo=sin token Endpoint=...` | Llegó una llamada sin token |
| `AUTH pidiendo token de servicio. ClientId=... Audiencia=...` | Un servicio pide un token a Keycloak |
| `AUTH token de servicio obtenido. ClientId=... Audiencia=... VenceEnSegundos=...` | Keycloak lo entregó |
| `AUTH falló el pedido de token de servicio. ClientId=... Audiencia=... Error=...` | Keycloak lo rechazó, no respondió o falta el secret |

Los tres logs de token de servicio salen de `AddTokenDeServicio`, es decir, del llamado de Booking a Payment. El `TokenServicio` de Availability es código propio: lo que loguea depende de él.

En `Quien=` aparece el usuario (`usuario1`) o, para tokens de servicio, el usuario de servicio del client (por ejemplo `service-account-booking-api`).

El token de servicio se **cachea** en memoria, uno por audiencia: se pide una vez y se reutiliza hasta 30 segundos antes de vencer. Por eso en el log no aparece un pedido por cada llamada.

## 9. Problemas frecuentes

| Síntoma | Causa probable | Qué hacer |
|---|---|---|
| 401 y log `Motivo=audiencia incorrecta` | El token es para otro servicio, o el `Audience` del `appsettings.json` no es el del propio servicio | Booking debe tener `booking-api` y Payment `payment-api`. Revisá también el argumento de `AddTokenDeServicio(...)` del que llama |
| 401 y log `Motivo=emisor incorrecto` | La `Authority` no coincide con la del token (por ejemplo `127.0.0.1` en vez de `localhost`) | Usá exactamente `http://localhost:8080/realms/reserva-asientos` en todos lados |
| 401 y log `Motivo=token inválido` con error `IDX20803` | La API no puede contactar a Keycloak | Verificá que Keycloak esté arriba y que la `Authority` esté bien escrita |
| Availability → Booking da 401 aunque el token es bueno | **Posible causa (no reproducida):** el `Program.cs` de Booking tiene `app.UseHttpsRedirection()`. Si Availability lo llama por `http://`, recibe una redirección a https y el header `Authorization` se puede perder | Correr Booking con el perfil `http` (`dotnet run --launch-profile http`) y volver a probar |
| Booking recibe `Unauthenticated` al llamar a Payment | Payment tiene la autenticación activada pero Booking todavía no le manda el token | Activá la autenticación de Payment recién cuando la llamada gRPC de Booking use `AddTokenDeServicio("payment-api")` (ver 5) |
| `Falta Keycloak:ClientSecret` | No hiciste `user-secrets set`, o no estás en `Development`. **Aparece al pedir el token de servicio, no al arrancar**, y queda también en el log como `AUTH falló el pedido de token de servicio` | Ver 3.5 |
| `unauthorized_client` al pedir un token | Secret incorrecto, o el client no tiene *Service accounts roles* | Verificá que el secret sea el de tu `.env`; si persiste, avisale a Dylan o Luciano |
| `invalid_scope` al pedir un token | El scope no existe, el client no lo tiene asignado, o tenés el realm viejo | Hacé `docker compose down` y `docker compose up -d` (ver 3.1). Si sigue, avisale a Dylan o Luciano |
| `invalid_redirect_uri` en el login de Availability | La URL del sitio no está registrada en el client `availability-web` | Pasale la URL a Dylan o Luciano (por ejemplo `https://localhost:7113/signin-oidc`) |
| `Correlation failed` en el login de Availability | Estás entrando por `http` | Correr Availability con `dotnet run --launch-profile https` y entrar por `https://localhost:7113` |
| Funciona en tu PC pero no en la de otro | Falta el secret en su máquina | Cada uno genera su `.env` y hace su propio `user-secrets set` |

## 10. Por qué está hecho así

Pensado para justificar las decisiones del TP (puntos 3.e y 3.f):

- **Librería compartida con métodos de extensión:** la seguridad se escribe una sola vez. Un cambio de política se hace en un solo lugar y los servicios no pueden configurarla mal por su cuenta.
- **Validación de audiencia:** un token emitido para Booking no sirve en Payment. Si un token se filtra, el daño queda limitado a un servicio.
- **Token de servicio con *client credentials*:** los microservicios se autentican entre sí con Keycloak sin intervención de un usuario (punto 3.e).
- **Cache del token de servicio:** evita una ida a Keycloak por cada llamada (menos latencia, menos carga y un punto de falla menos).
- **Tolerancia de reloj de 30 segundos:** el valor por defecto de .NET es 5 minutos, que con tokens de 5 minutos duplicaría su vida útil.
- **Logs de auditoría:** registran quién entró, a quién se rechazó y por qué, sin exponer el token (punto 3.f).
- **`RequireHttpsMetadata` según el entorno:** vale `!IsDevelopment()`, así que solo se desactiva en desarrollo (Keycloak corre en HTTP en local). Fuera de desarrollo se exige HTTPS.

## Checklist

**Booking**
- [x] Referencia a `Shared.Auth` y validación en `Program.cs`: ya están en `dev` (#84), no hay que agregarlas
- [ ] Paquetes `Grpc.Net.ClientFactory`, `Grpc.Tools` y `Google.Protobuf`, y el `<Protobuf ... GrpcServices="Client" />` en el `.csproj`
- [ ] `using Payment.Grpc;` y cliente gRPC hacia `http://localhost:5043` con `.AddTokenDeServicio("payment-api")` (pendiente de probar)
- [ ] `[Authorize]` en los endpoints que corresponden
- [ ] `ClientSecret` en `user-secrets` (el de `booking-api`)

**Payment**
- [ ] Booking ya manda el token por gRPC
- [ ] Referencia a `Shared.Auth` y `using Shared.Auth;`
- [ ] Líneas de autenticación del `Program.cs` y `[Authorize]` descomentados
- [ ] Decidido qué hacer con `/payment-test`

**Availability**
- [ ] Bases de datos levantadas y Availability corriendo con `--launch-profile https` (`https://localhost:7113`)
- [ ] `ClientSecret` en `user-secrets` (el de `availability-web`)
- [ ] Login probado con `usuario1` (y con GitHub, si se configuró)

**Todos**
- [ ] `.env` propio completado y Keycloak levantado con `down` y `up -d`
- [ ] Ningún secret ni `.env` en `git status`
