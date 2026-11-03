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
| Availability | `listar_eventos`, `listar_asientos`, `solicitar_reserva`, `consultar_solicitud` (detalle abajo) |
| Booking | A definir |
| Payment | A definir |

### Availability

| `Operacion` | `TraceId` | `Resultado` y `Motivo` |
|---|---|---|
| `listar_eventos` | Guid nuevo por pedido | `ok` · `error` (`excepcion`) |
| `listar_asientos` | Guid nuevo por pedido; viaja a Booking en `X-Trace-Id` | `ok` · `ok` con `ocupacion_no_disponible` si Booking no respondió (la página se muestra igual) · `rechazada` con `evento_inexistente` · `error` (`excepcion`) |
| `solicitar_reserva` | El `solicitudId`, que Availability genera al empezar la operación | `ok` · `rechazada` con `asiento_inexistente` · `error` con `broker_no_disponible` o `excepcion` |
| `consultar_solicitud` | El `solicitudId` consultado; viaja a Booking en `X-Trace-Id` | `ok` (cualquier estado, incluido "en proceso") · `rechazada` con `solicitud_inexistente` (también si es de otro usuario) · `error` con `booking_no_disponible` o `excepcion` |

El `Motivo` se registra como propiedad aparte en la línea de fin. Los casos de uso usan un puerto propio (`IAuditoria`) y un adaptador en `Availability.Gateways` llama a `IAuditLogger`, porque `Shared.Observability` depende de ASP.NET Core y los casos de uso no pueden referenciarlo sin romper Clean Architecture.