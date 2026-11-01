# Comunicación entre componentes

## 1. Flujo de comunicación

| Origen     | Destino       | Mecanismo            | Propósito                                                     |
|------------|---------------|----------------------|---------------------------------------------------------------|
| Cliente    | Authorization | Según implementación | Obtener token reserva                                         |
| Cliente    |  Availability | HTTP / REST          |Consultar disponibilidad, solicitar reserva y consultar estado |
|Availability| Authorization | Según implementación | Validar/autenticar la solicitud                               |
|Availability|  Broker       |Publicación de mensaje| Enviar la solicitud de reserva a Booking                      |
| Broker     |  Booking      | Consumo de mensaje   | Entregar la solicitud para su procesamiento                   |
| Booking    |  Payment      | gRPC                 | Procesar el pago de una reserva retenida                      |
| Booking    | Authorization | Según implementación | Autenticación entre microservicios                            |
| Payment    | Authorization | Jwt                  | Autenticación entre microservicios                            |
|Availability|  Booking      | HTTP / REST          | Consultar el resultado de una solicitud                       |
| Booking    | Notifications | Según implementación | Disparar la notificación de confirmación                      |

## 2. Mensaje de reserva

Availability debe publicar un mensaje que permita a Booking identificar y procesar la solicitud. Availability debe generar un identificador único para la solicitud.

| Campo                 | Descripción                               | Tipo / formato real |
|---------------------- |-------------------------------------------|-------------------  |
|Identificador solicitud| Identifica solicitud y consulta resultado | Completar           |
| Identificador evento  | Identifica evento                         | Completar           |
| Identificador asiento | Identifica asiento solicitado             | Completar           |
| Identificador usuario | Identifica solicitante                    | Completar           |
| Fecha de solicitud    | Momento de la solicitud                   | Completar           |


## 3. Contrato REST de Availability y Booking


| Servicio     |Método HTTP| Ruta      | Propósito                                   |
|---           |---        |---        |---                                          |
| Availability | Completar | Completar | Consultar asientos disponibles              |
| Availability | Completar | Completar | Enviar solicitud de reserva                 |
| Availability | Completar | Completar | Consultar resultado de solicitud            |
| Booking      | Completar | Completar | Operación REST implementada, si corresponde |

## 4. Contrato gRPC de Payment

El contrato gRPC está definido por el archivo: Payment/Payment.API/Protos/payment.proto

| Elemento                    | Definición                    |
| --------------------------- | ------------------------------|
| Servicio gRPC               | "Payment.Grpc"                |
| Método invocado por Booking | Completar                     |
| Mensaje de solicitud        | `ProcessPaymentRequest`       |
| Mensaje de respuesta        | `ProcessPaymentResult`        |
| Dirección / puerto          | Completar según configuración |

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

## 6. Auditoría y trazabilidad

Documentar los campos efectivamente registrados. Como mínimo, verificar si los logs incluyen:

- Identificador de correlación o solicitud.
- Servicio que genera el registro.
- Evento
- Fecha y hora.
- Inicio o finalización de la operación.
- Resultado o error.

El identificador de correlación permite relacionar registros de Availability, Booking y Payment cuando una solicitud atraviesa varios componentes, siempre que el sistema lo propague.

## 8. Consistencia eventual

Availability responde que la solicitud fue recibida antes de que Booking termine el procesamiento. Por eso, la respuesta inicial no significa que la reserva esté confirmada.

El usuario debe consultar posteriormente el resultado. La confirmación solo corresponde cuando Booking recibe un resultado de pago exitoso y actualiza el estado de la reserva.
