# Booking: solicitudes del broker y cobro a Payment

Booking recibe solicitudes de Availability, retiene el asiento y llama a Payment por gRPC para confirmar o rechazar la reserva (issues #12, #14 y #15). Los contratos están en `contracts/broker.md`, `contracts/broker-config.md` y `contracts/tokens.md`. La recuperación tiene limitaciones: no incluye conciliación ni devolución de pagos.

## 1. Qué hace el consumidor

1. Valida el cuerpo y los metadatos; envía los mensajes inválidos a la DLQ.
2. Reutiliza `RetencionReservas` para retener el asiento o rechazar por `asiento_ocupado`.
3. Si la reserva está retenida y vigente, cobra usando `Reserva.Id` como `booking_id`, también en los reintentos.
4. Confirma o rechaza mediante una actualización condicional: solo si sigue retenida y su vencimiento es posterior a la hora de PostgreSQL. También asigna `FechaActualizacion` con esa hora.
5. Completa el mensaje después de comprobar un resultado definitivo. Si el pago es incierto, no completa ni abandona; el consumidor sigue procesando otros mensajes.

`Booking.Api/Broker` es la entrada y crea un scope por mensaje. `Booking.Business/ProcesamientoSolicitudReserva` orquesta sin depender de Service Bus ni de los tipos gRPC. `Booking.Data/Payment` accede al servicio externo y devuelve un resultado propio. El cliente gRPC de Booking se genera únicamente en Data, enlazando el proto de Payment sin copiarlo ni modificarlo.

## 2. Configuración

| Clave | Valor |
|---|---|
| `ServiceBus:Enabled` | `false` por defecto; Booking arranca sin conectarse a Azure |
| `ServiceBus:ConnectionString` | Secreto entregado por Broker; nunca se versiona |
| `ServiceBus:TopicName` | `grupo7-booking-topic` |
| `ServiceBus:SubscriptionName` | Obligatoria al habilitar; `booking-<nombre>` en desarrollo y `booking` en la demo |
| `Booking:MontoReserva` | `1000`, positivo; el mensaje no contiene monto |
| `Payment:Address` | `http://localhost:5043`, HTTP/2 sin TLS para desarrollo |
| `Payment:DeadlineSeconds` | `10`; mayor que cero y menor que el lock de 60 segundos |
| `Keycloak:Authority` | `http://localhost:8080/realms/reserva-asientos` |
| `Keycloak:ClientId` | `booking-api` |
| `Keycloak:ClientSecret` | Secreto local del cliente Booking, correspondiente al realm |

Cambiar estos valores requiere reiniciar la API. La retención sigue configurada en 5 minutos.

### 2.1 Guardar la configuración

Desde la raíz del repo, en Development:

```powershell
dotnet user-secrets set "ServiceBus:Enabled" "false" --project src/Booking/Booking.Api
dotnet user-secrets set "ServiceBus:SubscriptionName" "booking-<nombre>" --project src/Booking/Booking.Api
dotnet user-secrets set "ServiceBus:ConnectionString" "<conexión entregada por Broker>" --project src/Booking/Booking.Api
dotnet user-secrets set "Keycloak:ClientSecret" "<secreto local de booking-api>" --project src/Booking/Booking.Api
```

`AddGrpcClient` utiliza `AddTokenDeServicio("payment-api")`. Antes de enviar el cobro se obtiene el token mediante el mismo proveedor y caché de Shared.Auth: permite distinguir una falla previa al envío de un error incierto durante gRPC. El handler adjunta `Authorization: Bearer`.

Mantener el consumo deshabilitado por defecto. Habilitarlo solo con servicios compatibles y pruebas coordinadas, considerando los pendientes de la sección 9.

### 2.2 Suscripción por integrante

Cada integrante usa su propia suscripción para no consumir mensajes ajenos. Publicar en el tópico genera copias en todas las suscripciones que acepten el mensaje, incluida `booking`; una suscripción propia no aísla la publicación.

## 3. Qué pasa con cada mensaje

Se exigen los cinco campos del contrato como texto y con valores válidos; los adicionales se ignoran. `fechaSolicitud` debe expresar UTC con `Z` o `+00:00`. Se validan `MessageId`, `CorrelationId`, `ContentType = application/json` y `Subject = solicitud-reserva`.

| Situación | Qué hace Booking |
|---|---|
| Cuerpo no JSON o UTF-8 inválido | DLQ con `json_invalido` |
| Campo o metadato obligatorio inválido | DLQ con `campo_invalido` |
| Mismo `solicitudId` con otro evento, asiento o usuario | DLQ con `campo_invalido`; conserva la reserva |
| Asiento ocupado | Rechaza con `asiento_ocupado` y completa |
| Reserva confirmada, rechazada o vencida | Completa sin llamar a Payment |
| Retenida cuya fecha ya venció, antes de que el worker cambie el estado | Completa sin llamar a Payment; el worker actualizará el estado |
| Retenida vigente, nueva o reentregada | Cobra con el mismo `Reserva.Id` |
| `Approved` con `booking_id` correcto | Confirma si la actualización condicional afecta una fila; luego completa |
| `Rejected` con `booking_id` correcto | Rechaza con `pago_fallido` si sigue vigente; luego completa |
| Reserva nueva: token no obtenido antes del envío, o `Unavailable` con `SocketException.ConnectionRefused` interno | Rechaza con `pago_no_disponible` si sigue vigente; luego completa |
| Reserva ya existente, retenida y vigente: Payment devuelve `NoDisponible` | Resultado incierto: conserva la retención, sin Complete ni Abandon |
| Deadline, conexión cortada, `Internal`, `Unknown`, `Unauthenticated`, estado desconocido o `booking_id` distinto | Resultado incierto: conserva la retención, sin Complete ni Abandon |
| Falla al guardar o verificar después del cobro | Resultado incierto; sin Complete ni Abandon |
| Falla transitoria antes del cobro | Conserva el manejo existente: Abandon para reentrega |
| Otra falla no resuelta | No completa; puede reentregarse al vencer el lock |

Si la actualización afecta cero filas, se consulta nuevamente el estado antes de completar. Si todavía está retenida y vigente, el resultado queda pendiente.

### 3.1 Reentregas y vencimiento

Una conexión rechazada solo demuestra que Payment no recibió el pedido actual, no que nunca haya cobrado la reserva. Por eso, si la reserva ya existía, `NoDisponible` se trata como incierto, incluso si el intento anterior se había cortado antes de llamar a Payment. Si no se recupera un resultado definitivo, esa reserva termina `vencida`, en lugar de rechazada con `pago_no_disponible`.

Un resultado incierto puede corresponder a un pago ya realizado. Se deja vencer el lock sin Abandon para evitar reentregas inmediatas que agoten el máximo en segundos. El consumidor sigue activo; una reentrega vigente vuelve a llamar a Payment con el mismo identificador.

El intervalo de reentrega no está garantizado. Cada vencimiento del lock consume una entrega y Azure tiene `MaxDeliveryCount = 10`: los reintentos no son ilimitados y el mensaje puede terminar en DLQ. Con una retención de 5 minutos, normalmente la reserva vence antes; la siguiente entrega se completa sin cobrar otra vez. Si Payment se corta durante la llamada y no se recupera un resultado, la reserva termina vencida, no rechazada por indisponibilidad segura.

Si llega un `Approved` cuando la reserva ya venció, Booking no confirma: completa el mensaje, registra una advertencia simple con ILogger y clasifica el resultado como error. **El pago requiere conciliación y devolución, que no están implementadas.**

También puede existir un pago aprobado desconocido si su resultado fue incierto y después venció la reserva. No hay consulta de pagos ni conciliación para resolverlo; completar la entrega vencida no demuestra que no hubo cobro.

## 4. Logs existentes

Se conserva el mecanismo de `Shared.Observability` y `contracts/logs.md`, sin nuevos eventos de auditoría ni interceptores de traza.

| `Operacion` | `TraceId` | `Resultado` y `Motivo` |
|---|---|---|
| `procesar_reserva` | `solicitudId`; Guid nuevo si no puede leerse | `ok`; `rechazada` con `asiento_ocupado`, `pago_fallido`, `pago_no_disponible`, `json_invalido` o `campo_invalido`; `error` con los motivos existentes, incluido `pago_pendiente` |
| `consumir_solicitudes` | Guid nuevo | `error` para fallas del transporte |

El fin sigue indicando si la solicitud era duplicada. Un pago aprobado que no puede confirmar una reserva vencida usa `error/fallo_procesamiento`, además de la advertencia simple. No se registran cuerpos, credenciales ni excepciones originales. La trazabilidad entre servicios y la auditoría del flujo integrado quedan pendientes.

## 5. Cómo probar con Azure

1. Coordinar con el grupo antes de publicar: las otras suscripciones también reciben copias.
2. Usar identificadores ficticios, una base temporal, servicios de prueba y la suscripción propia.
3. Conservar los MessageId y la evidencia.
4. Limpiar únicamente los mensajes identificados de la prueba en la suscripción propia y su DLQ.
5. Dejar las otras copias a sus responsables; no consumirlas ni eliminarlas sin autorización.

El manejador también puede probarse con un receptor de prueba, PostgreSQL real y Payment aislado, sin publicar en Azure. Comprueba decisiones de confirmación, pero no una reentrega real ni los tiempos del lock.

## 6. Qué está verificado

La integración se verificó con PostgreSQL 17.11, el realm del repositorio importado en Keycloak 26.8.1 y Payment del PR #83 en un worktree temporal. El compose de Auth define 26.0.8; esta prueba utilizó la imagen local 26.8.1, sin cambiar su configuración versionada. Se usaron datos sintéticos, credenciales efímeras y scripts fuera del repo; los contenedores y volúmenes existentes se conservaron.

| Prueba | Resultado |
|---|---|
| Pago aprobado real | Confirmada y Complete solicitado |
| Monto 20000 | Rechazada con `pago_fallido` y Complete |
| Solicitud nueva con Payment sin escuchar | Conexión rechazada real; `rechazada/pago_no_disponible` y Complete |
| Mismo solicitudId: primer intento incierto y segundo NoDisponible por conexión rechazada real | Una sola reserva, mismo BookingId, sigue `retenida` sin motivo de rechazo; cero Complete, Abandon y DLQ |
| Vencimiento después de ese reintento | Complete sin otra llamada a Payment; el worker la marca `vencida` |
| Deadline muy corto, con token ya obtenido | Retenida, sin Complete ni Abandon |
| Cobro previo por `/payment-test` y recuperación del mismo ID | Una fila en Payment, un evento de procesamiento y otro de duplicado ignorado; Booking confirma |
| Retenida con fecha vencida antes del worker | Cero llamadas a Payment y Complete |
| Vencimiento en PostgreSQL durante el cobro, con doble que responde Approved | No confirma, emite advertencia y completa |
| Token real de Keycloak | Scope `payment-api` y Bearer enviado con audiencia Payment y cliente Booking |
| Duplicado confirmado; resultado incierto seguido de vencimiento | No se vuelve a llamar a Payment |
| Solicitud nueva sin secreto para obtener token | Rechazo seguro sin fila creada en Payment |
| Once comprobaciones con dobles gRPC | Clasificación de errores, conexión rechazada/cortada, estados desconocidos y respuesta con ID distinto |
| Pago previo `Pending` | Payment vuelve a invocar su procesador; no acredita recuperación segura de un cobro anterior |
| SQL de confirmación/rechazo | Condición de vigencia y FechaActualizacion usan `now()` de PostgreSQL |
| Arranque con `ServiceBus:Enabled = false` | API operativa; OpenAPI responde 200 |

Para estos casos se usó un receptor de Service Bus de prueba: no se enviaron mensajes a Azure para verificar el cobro. El issue #12 ya verificó en Azure las DLQ por JSON/metadatos inválidos y la recepción con retención y TraceId; también comprobó duplicados y asiento ocupado con PostgreSQL real.

No se verificaron cobros simultáneos entre réplicas, renovación del lock en operaciones largas ni conciliación. No se incorpora un proyecto permanente de pruebas.

## 7. Problemas frecuentes

| Síntoma | Causa o interpretación |
|---|---|
| No llegan mensajes | Revisar Enabled, suscripción y publicaciones coordinadas |
| DLQ con `campo_invalido` | Revisar campos y los cuatro metadatos |
| `pago_no_disponible` | En una reserva nueva, falló la obtención previa del token o Payment rechazó la conexión; una reserva existente queda pendiente |
| Retenida con `pago_pendiente` | Resultado incierto; esperar reentrega, sin asegurar que Payment no cobró |
| Advertencia de pago aprobado sin confirmar | Inconsistencia que requiere intervención; no hay devolución automática |
| Arranque rechazado por opciones inválidas | Revisar monto positivo, dirección y deadline mayor que 0 y menor que 60 segundos |

## 8. Por qué está hecho así

- **Actualización condicional con hora de PostgreSQL:** el worker puede vencer la reserva durante el cobro; no alcanza con leer el estado anterior.
- **Identificador estable:** permite recuperar el resultado persistido en Payment; por sí solo no resuelve llamadas simultáneas ni pagos Pending.
- **Errores inciertos conservadores:** una falla de transporte no demuestra que no hubo cobro.
- **PeekLock y confirmación manual:** Complete depende de un resultado definitivo comprobado. Se mantienen Prefetch 0, una llamada concurrente por instancia y renovación automática máxima de 5 minutos.
- **Scopes y capas existentes:** cada procesamiento usa su DbContext; Data contiene gRPC y Business orquesta sin duplicar las reglas de retención.
- **Deshabilitado por defecto:** evita consumir mensajes compartidos al iniciar.

## 9. Pendientes

| Pendiente | Coordinación |
|---|---|
| Incorporar y validar Payment compatible con recuperación por BookingId del PR #83 | Payment; la versión de dev inspeccionada no tenía esa idempotencia |
| Resolver pagos Pending y llamadas simultáneas sin garantizar ausencia de cobros duplicados | Payment; el PR #83 recupera resultados finales, pero un Pending vuelve a procesarse |
| Conciliar pago aprobado con reserva vencida y pago incierto seguido de vencimiento | Booking y Payment; faltan consulta, conciliación y devolución |
| Solicitudes simultáneas entre réplicas | Guillermo, #17; RetencionReservas no se modificó |
| Validación del token en Payment y evolución del contrato gRPC | Payment y Autorización, #42 |
| Validación conjunta de la confirmación y sus límites de recuperación | Booking, Payment y Broker, #44 |
| Trazabilidad, auditoría del flujo integrado y prueba completa con Azure | Etapa posterior; no se agregaron interceptores ni se modificaron librerías compartidas |
