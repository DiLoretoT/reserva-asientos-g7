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
| Availability | A definir |
| Booking | A definir |
| Payment | A definir |