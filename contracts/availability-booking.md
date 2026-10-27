# Contrato REST entre Availability y Booking

Propuesta de la pareja de Availability para el issue #43. Queda abierta a los comentarios de la pareja de Booking en el pull request y entra en `dev` cuando las dos parejas la aprueben.

## Qué datos necesita Availability de Booking

Availability necesita dos datos que solo conoce el servicio que guarda las reservas, es decir Booking.

1. Availability consulta los asientos ocupados de un evento para mostrarles a los visitantes cuáles pueden reservar. Corresponde al paso 1 del enunciado, la consulta de asientos disponibles.
2. Availability consulta el estado de una solicitud de reserva para mostrarle al usuario cómo terminó su pedido. Corresponde al paso 7 del enunciado, la consulta del resultado.

En este contrato Availability llama y Booking responde. Booking no llama a Availability por REST.

La propuesta supone que cada solicitud reserva un solo asiento, como describe el paso 3 del enunciado.

## Criterios que comparten los dos endpoints

- Un asiento está ocupado si tiene una reserva retenida o confirmada, según la definición del enunciado. Una reserva retenida es la que Booking creó y todavía espera el resultado del pago. Una reserva rechazada o vencida no ocupa el asiento.
- Booking no sabe qué asientos existen en un evento, porque ese dato es de Availability. Por eso Booking informa solo los asientos ocupados, y Availability calcula los libres restando los ocupados de los asientos que tiene registrados.
- Los identificadores de evento, asiento y solicitud son de tipo `Guid`. Los genera Availability. Booking los recibe en el mensaje que Availability publica en el broker (Azure Service Bus).
- Las respuestas son JSON con nombres de propiedad en camelCase (ej `asientosOcupados`). Es el formato que ASP.NET Core usa por defecto.
- Los estados y los motivos viajan como texto en minúscula y con guion bajo entre palabras (`pago_fallido`), y no como números. Así el JSON se entiende sin conocer el código, y agregar un valor nuevo no cambia el significado de los que ya existen. ASP.NET Core escribe los enums como números por defecto, así que los dos servicios configuran el serializador con `JsonStringEnumConverter` y la política de nombres `JsonNamingPolicy.SnakeCaseLower`, disponible desde .NET 8.
- Booking declara los identificadores de la ruta como parámetros `Guid` sin restricción de tipo en la ruta. Con esa configuración, un valor que no es un `Guid` responde `400 Bad Request`. Si la ruta tuviera la restricción `{solicitudId:guid}`, un valor inválido respondería `404 Not Found` y se confundiría con el `404` del endpoint 2.
- El cuerpo de las respuestas de error no forma parte del contrato. Availability decide qué hacer solo a partir del código de estado.

## Autorización

Las dos llamadas llevan el token de servicio de Availability en el header `Authorization`. Si el token falta o no es válido, Booking responde `401 Unauthorized`.

```http
Authorization: Bearer <token de servicio de Availability>
```

El formato del token, cómo lo obtiene Availability, cómo lo valida Booking y si hace falta un `403 Forbidden` para un token válido sin permiso quedan pendientes del contrato de tokens (issue #45). El esquema `Bearer` del ejemplo es provisorio hasta que ese contrato se apruebe.

## Endpoint 1. Asientos ocupados de un evento

Devuelve los identificadores de los asientos del evento que tienen una reserva retenida o confirmada.

**Ruta**

```http
GET /api/eventos/{eventoId}/asientos-ocupados
```

**Parámetros**

| Parámetro | Ubicación | Tipo | Descripción |
|---|---|---|---|
| `eventoId` | ruta | `Guid` | Identificador del evento, asignado por Availability. |

**Respuesta exitosa**

```json
{
  "eventoId": "3f2b8c1e-6d4a-4b7e-9a21-5c8e0f1d2a34",
  "asientosOcupados": [
    "a1c4e7f0-2b5d-4e8a-9c3f-6d0b1e4a7c92",
    "b7d0a3c6-9e1f-4a2b-8d5c-0f3e6a9b2d18"
  ]
}
```

Si el evento no tiene asientos ocupados, `asientosOcupados` es una lista vacía y la respuesta sigue siendo `200 OK`.

**Códigos de estado**

| Código | Cuándo lo responde Booking |
|---|---|
| `200 OK` | Booking resolvió la consulta. La lista puede venir vacía. |
| `400 Bad Request` | El `eventoId` de la ruta no es un `Guid` válido. |
| `401 Unauthorized` | El token falta o no es válido. |

Este endpoint no responde `404 Not Found` para un evento desconocido. Booking no tiene la lista de eventos, así que no distingue un evento inexistente de un evento sin reservas, y en los dos casos devuelve la lista vacía. Verificar que el evento exista le corresponde a Availability.

## Endpoint 2. Estado de una solicitud de reserva

Devuelve el estado en que quedó la reserva creada a partir de una solicitud y, si se rechazó, el motivo.

**Ruta**

```http
GET /api/solicitudes/{solicitudId}
```

**Parámetros**

| Parámetro | Ubicación | Tipo | Descripción |
|---|---|---|---|
| `solicitudId` | ruta | `Guid` | Identificador único que Availability asignó a la solicitud al registrarla y publicarla en el broker. |

**Respuesta exitosa**

Ejemplo de una reserva confirmada:

```json
{
  "solicitudId": "c9e2f5a8-1b4d-4c7e-a0f3-8d6b9e2c5a71",
  "estado": "confirmada",
  "motivo": null
}
```

Ejemplo de una reserva rechazada:

```json
{
  "solicitudId": "c9e2f5a8-1b4d-4c7e-a0f3-8d6b9e2c5a71",
  "estado": "rechazada",
  "motivo": "pago_fallido"
}
```

**Valores de `estado`**

| Valor | Significado |
|---|---|
| `retenida` | Booking retuvo el asiento y espera el resultado del pago. |
| `confirmada` | El servicio Payment aprobó el pago y la reserva quedó confirmada. |
| `rechazada` | La reserva no se concretó. El campo `motivo` indica por qué. |
| `vencida` | La reserva retenida superó su tiempo de vencimiento sin resultado y el asiento se liberó. |

**Valores de `motivo`**

`motivo` tiene valor solo cuando `estado` es `rechazada`. En los otros tres estados es `null`.

| Valor | Significado |
|---|---|
| `asiento_ocupado` | El asiento ya tenía una reserva retenida o confirmada cuando Booking procesó la solicitud. |
| `pago_fallido` | Payment respondió que el pago no se aprobó. |
| `pago_no_disponible` | Payment no respondió a la llamada de cobro de Booking. |

**Códigos de estado**

| Código | Cuándo lo responde Booking |
|---|---|
| `200 OK` | Booking ya procesó la solicitud y devuelve su estado. |
| `400 Bad Request` | El `solicitudId` de la ruta no es un `Guid` válido. |
| `401 Unauthorized` | El token falta o no es válido. |
| `404 Not Found` | Booking todavía no procesó la solicitud. Availability la muestra como "en proceso". |

## Consistencia eventual entre la publicación y el procesamiento de una solicitud

Availability registra la solicitud en su base y la publica en el broker. Hasta que Booking toma ese mensaje y lo procesa, la solicitud no existe en Booking. Durante ese intervalo los dos servicios tienen información distinta sobre la misma solicitud, y llegan a tener la misma información cuando Booking termina de procesarla. Acá ubicamos la **consistencia eventual**.

El `404 Not Found` del endpoint 2 indica cómo interpretar la ausencia de la solicitud en Booking durante ese intervalo. Availability interpreta el `404` como "en proceso" y no como un error. Puede hacerlo porque solo pregunta por solicitudes que registró en su propia base antes de publicarlas, así que sabe que la solicitud existe aunque Booking todavía no la tenga.

El mismo intervalo afecta al endpoint 1. Supongamos que Availability le muestra a un usuario un asiento como libre y el usuario envía una solicitud para ese asiento. Si Booking procesa antes otra solicitud sobre el mismo asiento, rechaza la del usuario con el motivo `asiento_ocupado`, y el usuario ve el rechazo al consultar el estado.

## Temas abiertos para acordar con Booking

- Las dos parejas acuerdan la dirección base de Booking (host y puerto) en desarrollo y en la demo. Este documento usa rutas relativas hasta definirla.
- Las dos parejas acuerdan si Booking responde otros códigos cuando falla por un problema propio, por ejemplo `500 Internal Server Error` o `503 Service Unavailable`, y cómo los muestra Availability.
- Booking define cuánto dura una reserva retenida y si un asiento cuya retención ya venció deja de figurar en `asientosOcupados` aunque el proceso de vencimiento todavía no la haya marcado como `vencida`.
- Booking define qué pasa primero cuando Payment no responde, el rechazo con `pago_no_disponible` o el vencimiento de la retención.
- Availability define cuánto tiempo muestra una solicitud como "en proceso" antes de tratarla como fallida, por si el mensaje nunca llega a Booking.
- Las dos parejas acuerdan si alguno de los endpoints devuelve más datos, por ejemplo el `asientoId` o la fecha de vencimiento de una reserva retenida.
- Las dos parejas acuerdan cómo viaja el trace ID en las llamadas REST, para que los logs de Booking se puedan unir con los de Availability. Una opción es un header `X-Trace-Id`: en el endpoint 2 lleva el `solicitudId`, y en el endpoint 1 un GUID nuevo por consulta. Depende del formato de logs que definan Facundo y Gianluca.