# Booking

## 1. Responsabilidad

Booking es el microservicio que administra las reservas. Recibe cada solicitud de reserva que Availability publica en el broker, decide si el asiento está libre, cobra la reserva por medio de Payment y guarda el resultado. Expone una API REST para que Availability consulte los asientos ocupados de un evento y el estado de una solicitud.

Una reserva nace de una solicitud. Si el asiento está libre queda retenida, y si está ocupado se rechaza. Después del cobro pasa a confirmada o rechazada, y si el tiempo de retención se agota antes de un resultado, pasa a vencida. Booking no conoce los eventos ni los asientos: son de Availability, y por eso los guarda solo como identificadores.

## 2. Arquitectura

El enunciado pide que Booking sea una REST API con arquitectura N-Layer y dominio anémico. Cada capa es un proyecto de .NET, y cada una solo conoce a las de abajo.

```
src/Booking/
├── Booking.Api/
│   ├── Controllers/
│   │   └── ReservasController.cs
│   ├── Respuestas/
│   │   └── AsientosOcupadosRespuesta.cs
│   ├── Broker/
│   │   ├── ConsumidorSolicitudesServiceBus.cs
│   │   ├── ManejadorSolicitudServiceBus.cs
│   │   ├── ValidadorSolicitudServiceBus.cs
│   │   ├── ServiceBusExtensions.cs
│   │   └── ServiceBusOptions.cs
│   ├── VencimientoReservasService.cs
│   ├── BookingJson.cs
│   ├── Program.cs
│   └── appsettings.json
│
├── Booking.Business/
│   ├── ProcesamientoSolicitudReserva.cs
│   ├── RetencionReservas.cs
│   ├── ConsultaReservas.cs
│   ├── VencimientoReservas.cs
│   ├── SolicitudReserva.cs
│   ├── ResultadoProcesamientoReserva.cs
│   ├── AsientoOcupado.cs
│   ├── EstadoSolicitud.cs
│   └── BookingOptions.cs
│
├── Booking.Data/
│   ├── BookingDbContext.cs
│   ├── Migrations/
│   └── Payment/
│       ├── IClientePayment.cs
│       ├── ClientePayment.cs
│       └── PaymentOptions.cs
│
└── Booking.Domain/
    ├── Reserva.cs
    ├── EstadoReserva.cs
    └── MotivoRechazo.cs
```

Las referencias entre proyectos son `Booking.Api` → `Booking.Business` → `Booking.Data` → `Booking.Domain`. `Booking.Api` también referencia `Booking.Data` porque `Program.cs` registra el `DbContext` y el cliente de Payment, pero las reglas pasan siempre por `Booking.Business`. Además usa dos proyectos compartidos: `Shared.Auth` para validar los tokens y `Shared.Observability` para los logs.

## 3. Responsabilidad de las capas

### Booking.Api

Es la capa de entrada del microservicio. Recibe pedidos por tres vías: HTTP, mensajes del broker y un temporizador.

- `Controllers/ReservasController.cs`: expone los dos endpoints REST. Es delgado: llama a `ConsultaReservas` y devuelve el código de estado. Lleva `[Authorize]`, y registra el inicio y el fin de cada operación en la auditoría.
- `Respuestas/AsientosOcupadosRespuesta.cs`: el cuerpo JSON del primer endpoint. Vive en esta capa porque es parte del formato HTTP del contrato.
- `Broker/`: el consumidor de Azure Service Bus. `ConsumidorSolicitudesServiceBus` es el servicio en segundo plano que recibe los mensajes, `ValidadorSolicitudServiceBus` revisa que cumplan el contrato y `ManejadorSolicitudServiceBus` los procesa y decide si se completa, se reentrega o va a la cola de mensajes fallidos (DLQ). Solo se activa con `ServiceBus:Enabled`.
- `VencimientoReservasService.cs`: servicio en segundo plano que dispara el vencimiento cada `Booking:ExpirationIntervalSeconds`. Solo llama a `VencimientoReservas`.
- `BookingJson.cs`: configura cómo se escribe el JSON.
- `Program.cs`: configura la aplicación, valida la configuración al arrancar y registra las dependencias, la autenticación y los logs. En Development aplica las migraciones.

### Booking.Business

Contiene las reglas de Booking. Usa el `DbContext` directo, sin repositorios, porque EF Core ya es la capa de acceso a los datos y otra capa encima solo duplicaría código.

- `ProcesamientoSolicitudReserva.cs`: coordina todo el procesamiento de una solicitud. Reutiliza la reserva si ya existe (mensaje duplicado), llama a `RetencionReservas`, cobra con Payment y deja la reserva confirmada o rechazada.
- `RetencionReservas.cs`: decide si el asiento está libre. Inserta una reserva retenida y deja que lo decida el índice único de la base, así dos réplicas no pueden retener el mismo asiento. Si la solicitud ya tenía reserva, devuelve esa.
- `ConsultaReservas.cs`: las dos consultas del contrato. Un asiento está ocupado si su reserva está confirmada, o retenida con el vencimiento sin cumplir. La hora se compara con la de la base de datos.
- `VencimientoReservas.cs`: pasa a vencida las reservas retenidas cuyo vencimiento ya pasó.
- `SolicitudReserva.cs`: los datos de la solicitud que llegan del broker. Es de esta capa para que no dependa del formato del mensaje.
- `ResultadoProcesamientoReserva.cs`: lo que devuelve el procesamiento al consumidor: finalizada, rechazada, pendiente de pago o conflicto.
- `AsientoOcupado.cs` y `EstadoSolicitud.cs`: lo que devuelven las dos consultas, sin exponer la entidad `Reserva`.
- `BookingOptions.cs`: configuración de la retención, el intervalo de vencimiento y el monto.

### Booking.Data

Implementa los detalles técnicos que necesita la capa de negocio.

- `BookingDbContext.cs`: el contexto de EF Core. Declara el mapeo a PostgreSQL, las restricciones de integridad y los índices.
- `Migrations/`: las migraciones de EF Core.
- `Payment/`: el cliente gRPC hacia Payment. `ClientePayment` llama a `ProcessPayment` con un plazo máximo, y traduce la respuesta a aprobado, rechazado, no disponible o incierto.

### Booking.Domain

Contiene la entidad y los estados de la reserva. Es anémica: solo tiene datos, y las reglas están en `Booking.Business`.

- `Reserva.cs`: la reserva, con su solicitud, evento, asiento, usuario, estado, motivo y fechas.
- `EstadoReserva.cs`: retenida, confirmada, rechazada y vencida.
- `MotivoRechazo.cs`: asiento ocupado, pago fallido y pago no disponible.

## 4. Flujo de procesamiento

1. Availability publica la solicitud en el broker y Azure Service Bus la entrega a Booking.
2. `ConsumidorSolicitudesServiceBus` recibe el mensaje y `ValidadorSolicitudServiceBus` revisa que cumpla el contrato. Si no, va a la DLQ sin crear ni modificar reservas.
3. `ManejadorSolicitudServiceBus` pasa la solicitud a `ProcesamientoSolicitudReserva`.
4. Si la solicitud ya tiene reserva, se reutiliza. Si no, `RetencionReservas` inserta una reserva retenida, o la rechaza con `asiento_ocupado` si el índice único indica que el asiento está ocupado.
5. Si la reserva quedó retenida, `ClientePayment` llama a Payment por gRPC, con un token de servicio.
6. Con la respuesta de Payment, la reserva pasa a confirmada o a rechazada con `pago_fallido` o `pago_no_disponible`. Si el resultado es incierto, queda pendiente para no cobrar dos veces.
7. El manejador completa el mensaje, lo deja para que se reentregue si el error fue transitorio, o lo manda a la DLQ si es inválido.
8. En paralelo, `VencimientoReservasService` pasa a vencida cada reserva retenida cuyo plazo terminó, y el asiento vuelve a estar libre.
9. Availability consulta el resultado por REST cuando el usuario lo pide.

## 5. Contrato REST

Los dos endpoints están definidos en [`contracts/availability-booking.md`](../../contracts/availability-booking.md) y los protege `[Authorize]`: sin un token válido de Keycloak con la audiencia `booking-api` responden 401.

| Método | Ruta | Respuesta |
|---|---|---|
| GET | `/api/eventos/{eventoId}/asientos-ocupados` | 200 con los asientos ocupados del evento y el estado de su reserva. La lista puede venir vacía. |
| GET | `/api/solicitudes/{solicitudId}` | 200 con el estado de la solicitud y el motivo de rechazo si lo hay. 404 si Booking todavía no la procesó. |

Un id que no es un `Guid` responde 400. El 404 existe por la consistencia eventual: Availability responde "recibida" antes de que Booking tome el mensaje, así que por un rato la solicitud existe allá y no acá. Una reserva retenida que ya venció se informa como vencida aunque el proceso de vencimiento no haya corrido.

## 6. Contrato con Payment

Booking es el cliente del servicio gRPC `PaymentGrpc` y llama al método `ProcessPayment` con el id de la reserva y el monto. La dirección sale de `Payment:Address` (`http://localhost:5043` en desarrollo) y cada llamada tiene un plazo máximo de `Payment:DeadlineSeconds`. El token de servicio se pide a Keycloak con el client `booking-api` y la audiencia `payment-api`.

## 7. Persistencia

La persistencia se organiza con `BookingDbContext` de EF Core.

- Motor de base de datos utilizado: PostgreSQL 17, en Docker con `compose.yaml` (servicio `booking-db`).
- Nombre: `booking_db`.
- Tabla: `reservas`, una sola, porque cada solicitud genera una reserva que cambia de estado.
- Integridad: la hace cumplir la base de datos y no el código. La restricción única de `solicitud_id` impide dos reservas de la misma solicitud, y el índice único parcial `ux_reserva_asiento_activo` impide dos reservas retenidas o confirmadas del mismo asiento. También hay restricciones que validan los estados, el motivo de rechazo y el vencimiento.
- Las fechas de creación y actualización las completa la base con su propia hora, porque hay varias réplicas y sus relojes pueden diferir.

La configuración y las verificaciones manuales están en [`booking-postgresql.md`](../booking-postgresql.md).

## 8. Concurrencia y duplicados

Varias réplicas de Booking pueden recibir mensajes al mismo tiempo, y el broker puede entregar el mismo mensaje más de una vez. Como las réplicas solo comparten la base de datos, la garantía está en la base: se inserta directo y deciden las restricciones únicas, sin una consulta previa que otra réplica podría esquivar. La réplica que pierde una carrera por la misma solicitud lee la reserva de la ganadora y devuelve esa, así que una solicitud nunca genera dos reservas. El detalle está en [`../Documentacion/decisiones.md`](../Documentacion/decisiones.md).

## 9. Auditoría

Booking usa `Shared.Observability` y escribe logs estructurados en la consola y en Seq, con el campo `Servicio` igual a `booking`. Cada operación registra "[Inicio Operación]" y "[Fin Operación]", también cuando falla, con el `Resultado` y el `Motivo` en la línea de fin.

| Operación | TraceId |
|---|---|
| `procesar_reserva` | El `solicitudId` del mensaje. |
| `consultar_asientos_ocupados` | El `X-Trace-Id` del pedido, o un Guid nuevo si no viene. |
| `consultar_solicitud` | El `X-Trace-Id` del pedido, que manda Availability con el `solicitudId`. |

Los resultados posibles y sus motivos están en [`contracts/logs.md`](../../contracts/logs.md).
