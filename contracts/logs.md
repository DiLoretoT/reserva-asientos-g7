# Contrato de logs y traza

Módulo: Broker, Logs e Integración · Issue #37
Responsable: Gianluca · Revisa: Facundo

Este contrato lo cumplen Availability, Booking y Payment. Lo implementa `Shared.Observability`.

## 1. Trace ID

- El `TraceId` es el `solicitudId` de la solicitud de reserva. Es un `Guid`.
- No existe un identificador de traza aparte en el mensaje del broker.
- Si una operación no nace de una solicitud (por ejemplo `GET /api/eventos/{id}/asientos-ocupados`), el servicio genera un `Guid` nuevo por request.
- Si el request ya trae el header, se respeta ese valor; si no, se genera uno.

| Canal | Dónde viaja |
|---|---|
| REST | Header `X-Trace-Id` |
| gRPC | Metadata `x-trace-id` (en minúscula, lo exige gRPC) |
| Broker | El `solicitudId` del mensaje |

## 2. Campos obligatorios de cada línea de log

Los logs son estructurados (Serilog) y se envían a Seq.

| Campo | Descripción |
|---|---|
| `Timestamp` | Fecha y hora en UTC (lo agrega Serilog) |
| `Level` | Nivel del log |
| `TraceId` | El identificador de la sección 1 |
| `Servicio` | `availability`, `booking` o `payment` |
| `Operacion` | Nombre de la operación en snake_case (ej. `solicitar_reserva`, `procesar_reserva`, `cobrar`) |
| `Resultado` | Presente en los logs de fin y en errores (valores en la sección 4) |

## 3. Auditoría: inicio y fin

Cada operación relevante genera dos eventos con el mismo `TraceId` y la misma `Operacion`:

- `[Inicio Operación]`: al comenzar.
- `[Fin Operación]`: al terminar, con el `Resultado`. Se registra también cuando la operación falla.

Se auditan, como mínimo, las operaciones que hacen avanzar una reserva en cada servicio.

## 4. Valores de `Resultado` (propuesta)

`ok`, `rechazada`, `error`.

El motivo de rechazo usa los textos del contrato del broker: `asiento_ocupado`, `pago_fallido`, `pago_no_disponible`.

## 5. Niveles

| Nivel | Uso |
|---|---|
| Debug | Detalle de desarrollo. No se envía en producción |
| Information | Inicio y fin de operaciones, hitos normales |
| Warning | Situaciones anómalas que se controlan (reintentos, circuito abierto) |
| Error | Fallas no esperadas, con la excepción |

## 6. Qué NO se loguea

- Cuerpos de mensajes, requests ni responses (pueden contener datos personales).
- Cadenas de conexión, tokens, secretos ni contraseñas.
- Datos personales del usuario más allá de su identificador.

## 7. Qué se registra en cada servicio (a completar con cada pareja)

| Servicio | Operaciones auditadas |
|---|---|
| Availability | `listar_eventos`, `listar_asientos`, `solicitar_reserva`, `reintentar_envio`, `consultar_solicitud` (detalle abajo) |
| Booking | `procesar_reserva`, `consultar_asientos_ocupados`, `consultar_solicitud` (detalle abajo) |
| Payment | A definir |

### Availability

| `Operacion` | `TraceId` | `Resultado` y `Motivo` |
|---|---|---|
| `listar_eventos` | Guid nuevo por pedido | `ok` · `error` (`excepcion`) |
| `listar_asientos` | Guid nuevo por pedido; viaja a Booking en `X-Trace-Id` | `ok` · `ok` con `ocupacion_no_disponible` si Booking no respondió (la página se muestra igual) · `rechazada` con `evento_inexistente` · `error` (`excepcion`) |
| `solicitar_reserva` | El `solicitudId`, que Availability genera al empezar la operación | `ok` · `rechazada` con `asiento_inexistente` · `error` con `broker_no_disponible` (la solicitud queda guardada como pendiente de envío) o `excepcion` |
| `reintentar_envio` | El `solicitudId` de la solicitud pendiente que se reenvía | `ok` cuando el broker confirma la publicación · `error` con `broker_no_disponible` (sigue pendiente) o `excepcion` |
| `consultar_solicitud` | El `solicitudId` consultado; viaja a Booking en `X-Trace-Id` | `ok` (cualquier estado, incluidos "en proceso" y "pendiente de envío") · `rechazada` con `solicitud_inexistente` (también si es de otro usuario) · `error` con `booking_no_disponible` o `excepcion` |

El `Motivo` se registra como propiedad aparte en la línea de fin. `reintentar_envio` la ejecuta un proceso en segundo plano de Availability cada 15 segundos, sobre las solicitudes que quedaron pendientes de envío porque el broker no respondió (patrón Outbox). En cada vuelta reenvía las pendientes registradas hace más de 90 segundos, de la más vieja a la más nueva, y se detiene en la primera que el broker no confirma. Cada intento registra su propio `reintentar_envio`. Reenvía el mensaje con el mismo `solicitudId`, que es el `MessageId` del contrato del broker. Usa ese `solicitudId` como `TraceId`, así el reenvío queda en la misma cadena de sucesos que el `solicitar_reserva` que creó la solicitud.

Los casos de uso usan un puerto propio (`IAuditoria`) y un adaptador en `Availability.Gateways` llama a `IAuditLogger`, porque `Shared.Observability` depende de ASP.NET Core y los casos de uso no pueden referenciarlo sin romper Clean Architecture.

### Booking

| `Operacion` | `TraceId` | `Resultado` y `Motivo` |
|---|---|---|
| `procesar_reserva` | El `solicitudId` del mensaje del broker; si el mensaje es inválido y no trae uno, un Guid nuevo | `ok` con `procesada` · `rechazada` con `asiento_ocupado`, `pago_fallido`, `pago_no_disponible` o el motivo de DLQ (`json_invalido` o `campo_invalido`) · `error` con `pago_pendiente` (pago incierto), `cancelada`, `reentrega_pendiente`, `fallo_transitorio`, `fallo_procesamiento` o `excepcion`. Agrega la propiedad `Duplicado` |
| `consultar_asientos_ocupados` | El `X-Trace-Id` del pedido, o un Guid nuevo si no viene | `ok` · `error` (`excepcion`) |
| `consultar_solicitud` | El `X-Trace-Id` del pedido (Availability manda el `solicitudId`), o un Guid nuevo si no viene | `ok` · `rechazada` con `solicitud_inexistente` (Booking todavía no la procesó, es el 404) · `error` (`excepcion`) |

Un 401 por token inválido lo resuelve el middleware de autenticación antes del controller y no genera auditoría de Booking. El vencimiento automático de reservas no se audita: no nace de una solicitud ni de un pedido y corre cada pocos segundos casi siempre sin hacer nada.
