# Contrato de logs y traza

**Módulo:** Broker, Logs e Integración · Issue #37  
**Responsable:** Gianluca · **Revisa:** Facundo

Este documento describe los criterios de trazabilidad y logging de los servicios Availability, Booking y Payment. La infraestructura compartida de observabilidad se encuentra en `Shared.Observability`. 

## Componentes compartidos: Shared.Observability

El proyecto `Shared.Observability` centraliza los componentes utilizados por los servicios para implementar la observabilidad y la trazabilidad.

Incluye:

- `ITraceContext` y `TraceContext`: permiten mantener el `TraceId` durante el procesamiento de una operación.
- `TraceIdEnricher`: incorpora el `TraceId` como propiedad de los eventos de log cuando está disponible.
- `IAuditLogger` y `AuditLogger`: permiten registrar eventos de inicio y fin de operaciones.
- Middlewares HTTP: establecen el contexto de trazabilidad a partir del header `X-Trace-Id`.
- Interceptores gRPC: permiten transmitir y recuperar el `TraceId` mediante metadata.
- `ObservabilityExtensions`: centraliza el registro de dependencias y la configuración de Serilog y Seq.

## 1. TraceId

El `TraceId` permite relacionar los logs correspondientes a una misma solicitud u operación.

- Para una solicitud de reserva, el `TraceId` corresponde al `solicitudId`, que es un `Guid`.
- No existe un identificador de traza separado en el mensaje del broker.
- Para operaciones que no nacen de una solicitud de reserva, se genera un `Guid` nuevo por request.
- Si un request REST recibe un header `X-Trace-Id` válido, se conserva ese valor. Si no lo recibe, se genera uno nuevo.

| Canal | Dónde viaja |
|---|---|
| REST | Header `X-Trace-Id` |
| gRPC | Metadata `x-trace-id` |
| Broker | El `solicitudId` del mensaje |

## 2. Logging y envío a Seq

Los servicios utilizan Serilog para generar logs estructurados y enviarlos a Seq.

Se incorporan las siguientes propiedades:

| Campo | Descripción |
|---|---|
| `Timestamp` | Fecha y hora del evento. |
| `Level` | Nivel del log. |
| `Servicio` | Identifica al servicio que genera el evento: `availability`, `booking` o `payment`. |
| `TraceId` | Identificador de la traza, cuando existe un contexto de trazabilidad disponible. |

El `TraceId` se agrega a los eventos mediante `TraceIdEnricher`. Si no hay un TraceId establecido en el contexto, la propiedad no se agrega.

La propiedad `Servicio` permite distinguir en Seq de qué servicio proviene cada evento.

## 3. Propagación del TraceId en Booking y Payment

La implementación de observabilidad contempla los siguientes mecanismos:

### Booking

- `UseTraceId`: middleware que obtiene el `TraceId` del header `X-Trace-Id` o genera uno nuevo.
- `ITraceContext`: mantiene el identificador disponible durante el procesamiento de la operación.
- `TraceIdGrpcClientInterceptor`: agrega el `TraceId` a la metadata de las llamadas gRPC dirigidas a Payment.

### Payment

- `UseTraceId`: middleware que establece el contexto de trazabilidad para los requests HTTP.
- `TraceIdGrpcInterceptor`: recupera el `TraceId` recibido mediante metadata gRPC. Si no recibe uno válido, genera uno nuevo.
- `ITraceContext`: mantiene el identificador disponible durante el procesamiento de la llamada.

De esta manera, Booking puede transmitir el identificador de una operación a Payment mediante gRPC, permitiendo relacionar los eventos generados por ambos servicios.

## 4. Auditoría de operaciones

Se dispone de la interfaz `IAuditLogger` y su implementación `AuditLogger` dentro de `Shared.Observability`.

Esta implementación ofrece métodos para registrar:

- `[Inicio Operación]`: inicio de una operación.
- `[Fin Operación]`: finalización de una operación.

La existencia de esta herramienta no implica que todas las operaciones de los servicios estén instrumentadas con eventos de inicio y fin.

Por el momento, no se establece como obligatorio que todos los logs tengan las propiedades `Operacion` y `Resultado`, ya que no se implementó una estandarización de esos campos en todos los servicios.

## 5. Niveles de log

| Nivel | Uso |
|---|---|
| Debug | Información detallada para desarrollo. |
| Information | Eventos informativos y funcionamiento normal. |
| Warning | Situaciones anómalas que pueden controlarse. |
| Error | Errores y fallas durante el procesamiento. |

La configuración de Serilog establece el nivel mínimo `Information`.

## 6. Qué NO se loguea

- Cuerpos completos de mensajes, requests ni responses que puedan contener datos personales.
- Cadenas de conexión, tokens, secretos ni contraseñas.
- Datos personales del usuario más allá de la información estrictamente necesaria.

## 7. Qué se registra en cada servicio

| Servicio | Alcance documentado |
|---|---|
| Availability | Operaciones de consulta de eventos y asientos, solicitudes de reserva y reintentos de envío. |
| Booking | Requests HTTP, procesamiento de solicitudes de reserva y llamadas gRPC a Payment. |
| Payment | Requests HTTP, procesamiento de llamadas gRPC y eventos del servicio. |

### Availability

| `Operacion` | `TraceId` | `Resultado` y `Motivo` |
|---|---|---|
| `listar_eventos` | Guid nuevo por pedido | `ok` · `error` (`excepcion`) |
| `listar_asientos` | Guid nuevo por pedido; viaja a Booking en `X-Trace-Id` | `ok` · `ok` con `ocupacion_no_disponible` si Booking no respondió (la página se muestra igual) · `rechazada` con `evento_inexistente` · `error` (`excepcion`) |
| `solicitar_reserva` | El `solicitudId`, que Availability genera al empezar la operación | `ok` · `rechazada` con `asiento_inexistente` · `error` con `broker_no_disponible` (la solicitud queda guardada como pendiente de envío) o `excepcion` |
| `reintentar_envio` | El `solicitudId` de la solicitud pendiente que se reenvía | `ok` cuando el broker confirma la publicación · `error` con `broker_no_disponible` (sigue pendiente) o `excepcion` |
| `consultar_solicitud` | El `solicitudId` consultado; viaja a Booking en `X-Trace-Id` | `ok` (cualquier estado, incluidos "en proceso" y "pendiente de envío") · `rechazada` con `solicitud_inexistente` (también si es de otro usuario) · `error` con `booking_no_disponible` o `excepcion` |

El `Motivo` se registra como propiedad aparte en la línea de fin.

`reintentar_envio` la ejecuta un proceso en segundo plano de Availability cada 15 segundos, sobre las solicitudes que quedaron pendientes de envío porque el broker no respondió (patrón Outbox). En cada vuelta reenvía las pendientes registradas hace más de 90 segundos, de la más vieja a la más nueva, y se detiene en la primera que el broker no confirma.

Cada intento registra su propio `reintentar_envio`. Reenvía el mensaje con el mismo `solicitudId`, que es el `MessageId` del contrato del broker. Usa ese `solicitudId` como `TraceId`, así el reenvío queda en la misma cadena de sucesos que el `solicitar_reserva` que creó la solicitud.

Los casos de uso usan un puerto propio (`IAuditoria`) y un adaptador en `Availability.Gateways` llama a `IAuditLogger`, porque `Shared.Observability` depende de ASP.NET Core y los casos de uso no pueden referenciarlo sin romper Clean Architecture.

### Booking

La trazabilidad se establece mediante el middleware `UseTraceId` para los requests HTTP.

Durante el procesamiento de una solicitud recibida desde el broker, Booking utiliza el `solicitudId` como `TraceId`.

En las llamadas a Payment mediante gRPC, el interceptor `TraceIdGrpcClientInterceptor` transmite el identificador en la metadata `x-trace-id`.

Los logs se envían a Seq identificados con la propiedad `Servicio = booking`.

### Payment

Payment incorpora la configuración de observabilidad mediante `AddObservability("payment")`.

El middleware `UseTraceId` establece el contexto de trazabilidad para los requests HTTP, y el interceptor `TraceIdGrpcInterceptor` recupera el identificador recibido en las llamadas gRPC.

Los logs se envían a Seq identificados con la propiedad `Servicio = payment`.