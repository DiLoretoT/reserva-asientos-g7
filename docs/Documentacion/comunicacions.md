# Comunicación entre componentes

## 1. Flujo de comunicación

| Origen     | Destino       | Mecanismo            | Propósito                                                     |
|------------|---------------|----------------------|---------------------------------------------------------------|
| Cliente    | Authorization | OpenID Connect       | Iniciar sesión para poder reservar                            |
| Cliente    |  Availability | HTTP (páginas MVC)   |Consultar disponibilidad, solicitar reserva y consultar estado |
|Availability| Authorization | OpenID Connect y client credentials | Iniciar la sesión del usuario y obtener el token de servicio para llamar a Booking |
|Availability|  Broker       |Publicación de mensaje| Enviar la solicitud de reserva a Booking                      |
| Broker     |  Booking      | Consumo de mensaje   | Entregar la solicitud para su procesamiento                   |
| Booking    |  Payment      | gRPC                 | Procesar el pago de una reserva retenida                      |
| Booking    | Authorization | client credentials   | Obtener el token de servicio para llamar a Payment            |
| Payment    | Authorization | Jwt                  | Autenticación entre microservicios                            |
|Availability|  Booking      | HTTP / REST          | Consultar el resultado de una solicitud                       |
| Booking    | Notifications | Según implementación | Disparar la notificación de confirmación                      |

## 2. Mensaje de reserva

Availability debe publicar un mensaje que permita a Booking identificar y procesar la solicitud. Availability debe generar un identificador único para la solicitud.

| Campo                 | Descripción                               | Tipo / formato real |
|---------------------- |-------------------------------------------|-------------------  |
|Identificador solicitud| Identifica solicitud y consulta resultado | `solicitudId`, texto con un UUID (`Guid` en C#) |
| Identificador evento  | Identifica evento                         | `eventoId`, texto con un UUID (`Guid` en C#) |
| Identificador asiento | Identifica asiento solicitado             | `asientoId`, texto con un UUID (`Guid` en C#) |
| Identificador usuario | Identifica solicitante                    | `usuarioId`, texto (`string` en C#), el claim `sub` del usuario |
| Fecha de solicitud    | Momento de la solicitud                   | `fechaSolicitud`, texto ISO 8601 en UTC (`DateTime` en C#) |

El mensaje completo, con sus propiedades de Service Bus y sus validaciones, está en [`contracts/broker.md`](../../contracts/broker.md).


## 3. Contrato REST de Availability y Booking


| Servicio     |Método HTTP| Ruta      | Propósito                                   |
|---           |---        |---        |---                                          |
| Availability | GET       | `/Eventos/Asientos/{id}` | Consultar asientos disponibles |
| Availability | POST      | `/Reservas/Solicitar` | Enviar solicitud de reserva |
| Availability | GET       | `/solicitudes/{id}` | Consultar resultado de solicitud |
| Booking      | GET       | `/api/eventos/{eventoId}/asientos-ocupados` | Informar los asientos ocupados de un evento |
| Booking      | GET       | `/api/solicitudes/{solicitudId}` | Informar el estado de una solicitud (404 si todavía no la procesó) |

Las rutas de Availability son páginas del sitio MVC, no una API REST. Las llamadas de Availability a Booking están definidas en [`contracts/availability-booking.md`](../../contracts/availability-booking.md).

## 4. Contrato gRPC de Payment

El contrato gRPC está definido por el archivo: src/Payment/Payment.Grpc/Protos/Payment.proto

| Elemento                    | Definición                    |
| --------------------------- | ------------------------------|
| Servicio gRPC               | "Payment.Grpc"                |
| Método invocado por Booking | `ProcessPayment`              |
| Mensaje de solicitud        | `ProcessPaymentRequest`       |
| Mensaje de respuesta        | `ProcessPaymentReply`         |
| Dirección / puerto          | `Payment:Address` de Booking, `http://localhost:5043` en desarrollo |

### Secuencia esperada

1. Booking crea una reserva retenida.
2. Booking construye la solicitud de pago.
3. Booking invoca el método gRPC expuesto por Payment.
4. Payment procesa la solicitud.
5. Payment devuelve el resultado del procesamiento.
6. Booking interpreta el resultado y confirma o rechaza la reserva.

## 5. Autenticación entre microservicios

Documentar según la implementación:

- Cómo obtiene cada servicio su token.
- Cómo se transmite el token en las llamadas HTTP.
- Cómo se transmite el token en los metadatos de gRPC.
- Qué validaciones realiza cada servicio.
- Qué ocurre cuando el token falta, expiró o no es válido.

### Autenticación en Availability

El usuario inicia sesión en Keycloak con OpenID Connect, y Availability guarda la sesión en una cookie. La solicitud de reserva y el seguimiento llevan `[Authorize]`, así que un usuario sin sesión pasa primero por el inicio de sesión de Keycloak. Las páginas de eventos y asientos son públicas.

Para llamar a Booking, `TokenServicio` le pide a Keycloak un token con client credentials, con el client `availability-web` y el scope `booking-api`. Guarda ese token y lo renueva 30 segundos antes de que venza. `TokenServicioHandler` lo agrega, con el esquema `Bearer`, en el encabezado `Authorization` de cada petición que Availability hace a Booking.

Availability no expone ni consume gRPC, y sus páginas no aceptan tokens de otros servicios.

Si Keycloak o Booking no responden, o si Booking rechaza el token, `ClienteBooking` no propaga el error. El mapa de asientos se muestra con un aviso y los asientos sin estado, y el seguimiento muestra el estado como desconocido.

### Autenticación en Booking

Booking valida los tokens de Keycloak con `Shared.Auth` (`AddValidacionKeycloak`, con la audiencia `booking-api`), y `[Authorize]` protege sus dos endpoints. Sin token, con un token vencido o con uno de otra audiencia responde 401. Para llamar a Payment por gRPC, `AddTokenDeServicio("payment-api")` pide a Keycloak un token con client credentials, con el client `booking-api`, y lo agrega a los metadatos de cada llamada. Si Booking no puede obtener ese token, el pago queda como "no disponible" y la solicitud no crea un cobro. El consumidor del broker no usa token: se autentica con la conexión de Service Bus.

## 6. Auditoría y trazabilidad

Documentar los campos efectivamente registrados. Como mínimo, verificar si los logs incluyen:

- Identificador de correlación o solicitud.
- Servicio que genera el registro.
- Evento
- Fecha y hora.
- Inicio o finalización de la operación.
- Resultado o error.

El identificador de correlación permite relacionar registros de Availability, Booking y Payment cuando una solicitud atraviesa varios componentes, siempre que el sistema lo propague.

### Auditoría en Availability

Cada log de auditoría de Availability incluye `Timestamp`, `Level`, `TraceId`, `Servicio` con el valor `availability` y `Operacion`. Las líneas de fin agregan `Resultado`, y `Motivo` cuando corresponde. Las operaciones y sus resultados están en [`contracts/logs.md`](../../contracts/logs.md).

En la solicitud de reserva, el reintento de envío y el seguimiento, el `TraceId` es el `solicitudId`. Las peticiones a Booking llevan el `TraceId` en el encabezado `X-Trace-Id`, que agrega `TraceIdHttpClientHandler` de `Shared.Observability`.

### Auditoría en Booking

Booking usa `Shared.Observability` y escribe en la consola y en Seq con `Servicio` igual a `booking`. Registra el inicio y el fin de `procesar_reserva`, `consultar_asientos_ocupados` y `consultar_solicitud`, también cuando fallan, con `Resultado` y `Motivo` en la línea de fin. En `procesar_reserva` el `TraceId` es el `solicitudId` del mensaje, y en los endpoints es el `X-Trace-Id` del pedido, que manda Availability, o un Guid nuevo si no viene. Las operaciones y sus resultados están en [`contracts/logs.md`](../../contracts/logs.md).

## 8. Consistencia eventual

Availability responde que la solicitud fue recibida antes de que Booking termine el procesamiento. Por eso, la respuesta inicial no significa que la reserva esté confirmada.

El usuario debe consultar posteriormente el resultado. La confirmación solo corresponde cuando Booking recibe un resultado de pago exitoso y actualiza el estado de la reserva.
