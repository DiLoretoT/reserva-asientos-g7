# Decisiones de diseño y arquitectura

## 1. Arquitectura de microservicios

El sistema divide sus responsabilidades en servicios independientes: Availability, Booking, Payment y Notifications, además de los componentes de infraestructura requeridos.

Esta separación permite asignar responsabilidades específicas a cada servicio y definir contratos de comunicación entre ellos.

## 2. Clean Architecture en Availability

Availability debe organizarse siguiendo Clean Architecture. El objetivo del patrón es separar las reglas y casos de uso de los detalles externos, como HTTP, persistencia o mensajería.

Cada proyecto de .NET pertenece a un anillo, y una clase solo puede usar clases de otro proyecto si su `.csproj` tiene la referencia.

| Anillo | Proyecto | Referencia a |
|---|---|---|
| 1, Entities | `Availability.Entities` | Ningún proyecto |
| 2, Use Cases | `Availability.UseCases` | `Availability.Entities` |
| 3, Interface Adapters | `Availability.Presenters` | `Availability.UseCases` |
| 3, Interface Adapters | `Availability.Controllers` | `Availability.UseCases` y `Availability.Presenters` |
| 3, Interface Adapters | `Availability.Gateways` | `Availability.UseCases` y `Shared.Observability` |
| 4, Frameworks & Drivers | `Availability.Web` | `Availability.Controllers`, `Availability.Presenters`, `Availability.Gateways` y `Shared.Observability` |

El anillo 2 define las interfaces que necesita, y las clases de `Availability.Gateways` las implementan para acceder a la base, a Booking, al broker y a los logs. Los casos de uso registran la auditoría con su propia interfaz, `IAuditoria`, porque `Shared.Observability` depende de ASP.NET Core y el anillo 2 solo puede depender del anillo 1. El detalle de cada archivo está en [`../Microservicios/Availability.md`](../Microservicios/Availability.md).

## 3. Arquitectura N-Layer en Booking y Payment

Booking REST API y Payment gRPC API deben seguir una arquitectura N-Layer. La separación en capas permite organizar responsabilidades y reducir el acoplamiento entre la entrada de la aplicación, la lógica de aplicación y la infraestructura.

**En Booking, la estructura es:**
- `Booking.Api`: exposición de la API REST (los dos endpoints que consulta Availability), consumidor del broker y proceso en segundo plano del vencimiento.
- `Booking.Business`: reglas de la reserva (retener o rechazar, procesar el cobro, consultar y vencer reservas). Usa el `DbContext` directo, sin otra capa de repositorios encima.
- `Booking.Data`: `BookingDbContext` con EF Core y PostgreSQL, migraciones y el cliente gRPC hacia Payment.
- `Booking.Domain`: la entidad `Reserva` y sus enums de estado y motivo de rechazo. Es anémica, sin reglas.

Las capas se llaman en este sentido: `Api` → `Business` → `Data` → `Domain`. `Api` también registra `Data` al arrancar (el `DbContext` y el cliente de Payment), pero las reglas pasan siempre por `Business`. Las reglas de integridad que importan, como no tener dos reservas activas del mismo asiento, las hace cumplir la base de datos con índices y restricciones únicas, porque las réplicas de Booking solo comparten la base. El detalle de cada archivo está en [`../Microservicios/Booking.md`](../Microservicios/Booking.md).

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

**Contrato del proyecto:** `Payment/Payment.API/Protos/payment.proto`. Booking lo usa desde `Booking.Data/Payment` (`ClientePayment`), con un plazo máximo por llamada y su token de servicio.

## 7. Persistencia y SQLite

**Motor utilizado por servicio:**

| Servicio     | Motor     | Ubicación                                               |
|--------------|-----------|---------------------------------------------------------|
| Availability | SQLite    |`Availability/Availability.Gateways/DependencyInjection.cs`|
| Booking      | PostgreSQL 17 | `Booking/Booking.Api/Program.cs` y `Booking/Booking.Data/BookingDbContext.cs` |
| Payment      | SQLite    |`Payment/Payment.Data/Persintence/DependencyInjection.cs`|

## 8. Auditoría

Se agregan logs de auditoría para identificar el inicio y el fin de una cadena de sucesos en cada microservicio. Esto permite reconstruir el recorrido de una solicitud y analizar resultados o errores.

**Documentar el formato, destino y campos de los registros reales.**

Availability y Booking escriben logs estructurados con Serilog, por medio de `Shared.Observability`, en la consola y en Seq. Los campos y las operaciones están en [`contracts/logs.md`](../../contracts/logs.md).


## 9. Autenticación entre microservicios

Los microservicios deben autenticarse entre sí utilizando el servidor de autorización. 

Authorization Server (Keycloak)
Rol: autentica a los usuarios y a los servicios, y emite y firma los tokens. Tiene su propia base de datos.
Justificación: el enunciado pide que el usuario obtenga un token antes de reservar y que los microservicios se autentiquen entre sí con este servidor. Al ser un servidor independiente, las APIs no guardan contraseñas: solo validan tokens firmados.

3rd party Identity Provider (GitHub)
Rol: autentica al usuario que elige iniciar sesión con su cuenta de GitHub.
Justificación: figura en el diagrama. El usuario no necesita una contraseña propia en el sistema: GitHub confirma su identidad y Keycloak le emite su token, igual que a un usuario local. Los servicios no cambian.

Servidor de autorización y proveedor de identidad externo

Servidor: Keycloak 26.0.8, en Docker. Se usa un producto existente en lugar de programar el servidor, porque ya implementa OpenID Connect y OAuth 2.0, la emisión y firma de tokens y la gestión de usuarios. Tiene su propia base de datos. El detalle de cómo se piden y validan los tokens está en contracts/tokens.md.

Proveedor externo: GitHub. Keycloak hace de intermediario (identity brokering): el usuario inicia sesión en GitHub y Keycloak le emite su propio token. Los servicios siempre reciben tokens de Keycloak y no dependen de GitHub. Se eligió porque todos los integrantes tienen cuenta y registrar la aplicación es simple

## 10. Notificaciones

El sistema debe incorporar un servicio de notificaciones que informe al usuario cuando la reserva fue creada y confirmada luego de un pago exitoso.

**Completar el canal utilizado (correo electrónico, notificación web u otro), el contrato y el mecanismo de invocación según la implementación.**

## 11. Consistencia eventual

La solicitud puede haber sido recibida por Availability mientras todavía está pendiente de procesamiento en Booking. Por ello, la respuesta inicial indica recepción, no confirmación.

El estado se vuelve consistente con el resultado del procesamiento cuando Booking termina de gestionar la reserva y el pago. Documentar también cómo se manejan los mensajes duplicados, errores, reintentos y vencimientos si esas funcionalidades están implementadas.

### Reintento del envío en Availability

Availability aplica el patrón Outbox. Guarda la solicitud como pendiente de envío antes de publicarla y la marca como enviada cuando el broker confirma. Si el broker no confirma, la solicitud queda pendiente, y un servicio en segundo plano revisa cada 15 segundos las solicitudes pendientes y reenvía las que se registraron hace más de 90 segundos.

La alternativa era borrar la solicitud y pedirle al usuario que volviera a intentarlo. Se descartó porque un timeout no demuestra que el broker haya rechazado el mensaje. Si el mensaje hubiera llegado, Booking procesaría una reserva que Availability ya no conoce y que el usuario no podría seguir.

Cada reenvío conserva el mismo `solicitudId`, que también es el `MessageId` del mensaje, y Booking usa ese identificador para no procesar dos veces la misma solicitud.
