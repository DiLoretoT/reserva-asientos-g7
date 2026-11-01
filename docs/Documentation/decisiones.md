# Decisiones de diseño y arquitectura

## 1. Arquitectura de microservicios

El sistema divide sus responsabilidades en servicios independientes: Availability, Booking, Payment y Notifications, además de los componentes de infraestructura requeridos.

Esta separación permite asignar responsabilidades específicas a cada servicio y definir contratos de comunicación entre ellos.

## 2. Clean Architecture en Availability

Availability debe organizarse siguiendo Clean Architecture. El objetivo del patrón es separar las reglas y casos de uso de los detalles externos, como HTTP, persistencia o mensajería.

**Documentar aquí las capas y dependencias concretas que se hayan implementado en el servicio.**

## 3. Arquitectura N-Layer en Booking y Payment

Booking REST API y Payment gRPC API deben seguir una arquitectura N-Layer. La separación en capas permite organizar responsabilidades y reducir el acoplamiento entre la entrada de la aplicación, la lógica de aplicación y la infraestructura.

**En Booking, la estructura es:**
- 
- 
- 
- 

En Payment, la estructura es:
- `Payment.API`: exposición del servicio gRPC.
- `Payment.Business`: DTOs, interfaces y servicio de aplicación.
- `Payment.Data`: persistencia, procesamiento simulado y auditoría.
- `Payment.Domain`: entidades y estado del pago.

## 4. Dominio anémico

El dominio anémico representa las entidades principalmente mediante datos y propiedades, mientras que los casos de uso y servicios concentran la lógica de aplicación.

**La documentación debe reflejar cómo se distribuyen efectivamente las responsabilidades en cada servicio.**

## 5. Broker y procesamiento asincrónico

Availability publica una solicitud y responde sin esperar a que Booking termine de procesarla. Booking consume el mensaje y continúa el flujo.

Esta decisión desacopla la recepción de solicitudes de su procesamiento. También implica que el usuario debe consultar el resultado posteriormente y que el sistema debe contemplar estados intermedios.

**Tecnología elegida:** completar con el broker utilizado y justificar por qué fue seleccionado.

## 6. gRPC para la comunicación con Payment

Booking debe comunicarse con Payment mediante gRPC para solicitar el procesamiento del pago.

gRPC utiliza contratos definidos mediante archivos `.proto`, que describen los servicios y mensajes. Esto permite que ambos componentes compartan una definición explícita de la operación.

**Contrato del proyecto:** `Payment/Payment.API/Protos/payment.proto`.

## 7. Persistencia y SQLite

**Motor utilizado por servicio:**

| Servicio     | Motor     | Ubicación                                               |
|--------------|-----------|---------------------------------------------------------|
| Availability | Completar | Completar                                               |
| Booking      | Completar | Completar                                               |
| Payment      | SQLite    |`Payment/Payment.Data/Persintence/DependencyInjection.cs`|

## 8. Auditoría

Se agregan logs de auditoría para identificar el inicio y el fin de una cadena de sucesos en cada microservicio. Esto permite reconstruir el recorrido de una solicitud y analizar resultados o errores.

**Documentar el formato, destino y campos de los registros reales.**


## 9. Autenticación entre microservicios

Los microservicios deben autenticarse entre sí utilizando el servidor de autorización. 

## 10. Notificaciones

El sistema debe incorporar un servicio de notificaciones que informe al usuario cuando la reserva fue creada y confirmada luego de un pago exitoso.

**Completar el canal utilizado (correo electrónico, notificación web u otro), el contrato y el mecanismo de invocación según la implementación.**

## 11. Consistencia eventual

La solicitud puede haber sido recibida por Availability mientras todavía está pendiente de procesamiento en Booking. Por ello, la respuesta inicial indica recepción, no confirmación.

El estado se vuelve consistente con el resultado del procesamiento cuando Booking termina de gestionar la reserva y el pago. Documentar también cómo se manejan los mensajes duplicados, errores, reintentos y vencimientos si esas funcionalidades están implementadas.
