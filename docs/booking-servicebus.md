# Booking: consumidor de solicitudes del broker

Booking toma del broker (Azure Service Bus) las solicitudes de reserva que publica Availability y las procesa con la lógica de retención que ya existía. Corresponde al **paso 4 del enunciado** y al issue #12.

El formato del mensaje está en `contracts/broker.md` y la topología del broker en `contracts/broker-config.md` (PR #86). Esta etapa **no** incluye el cobro a Payment ni la confirmación de la reserva: eso es #14 y #15.

## 1. Qué hace el consumidor

1. Recibe el mensaje de la suscripción configurada.
2. Valida el cuerpo y los metadatos contra el contrato del broker.
3. Si el mensaje es inválido, lo manda a la DLQ con el motivo que corresponde.
4. Si es válido, llama a `RetencionReservas`: el asiento queda `retenida` o la solicitud queda `rechazada` con `asiento_ocupado`.
5. Registra el inicio y el fin de la operación con el `solicitudId` como TraceId.

El transporte vive en `Booking.Api/Broker`, que es la capa de entrada, igual que el controller. La lógica está en `Booking.Business` (`ProcesamientoSolicitudReserva`), así que la capa de negocio no depende de Service Bus.

## 2. Configuración

| Clave | Valor |
|---|---|
| `ServiceBus:Enabled` | `false` por defecto. Si está en `false` no se crea el cliente y Booking arranca sin credenciales de Azure |
| `ServiceBus:ConnectionString` | La entrega Broker. **Nunca va en el repo ni en el `appsettings.json`** |
| `ServiceBus:TopicName` | `grupo7-booking-topic` por defecto |
| `ServiceBus:SubscriptionName` | Obligatoria si `Enabled` es `true`. En desarrollo, tu suscripción `booking-<nombre>`; en la demo, `booking` |

Cambiar cualquiera de estos valores requiere reiniciar la API.

### 2.1 Guardar la configuración

Desde la raíz del repo:

```powershell
dotnet user-secrets set "ServiceBus:Enabled" "false" --project src/Booking/Booking.Api
dotnet user-secrets set "ServiceBus:SubscriptionName" "booking-<nombre>" --project src/Booking/Booking.Api
dotnet user-secrets set "ServiceBus:ConnectionString" "<la que te pasa Broker>" --project src/Booking/Booking.Api
```

Mantener `Enabled = false` hasta integrar Payment y acordar la recuperación y confirmación. Habilitarlo temporalmente solo para pruebas coordinadas y volver a deshabilitarlo al terminar.

Los user secrets solo se cargan en Development. El perfil `http` de `launchSettings.json` ya corre en Development.

### 2.2 Suscripción por integrante

En desarrollo, cada uno usa **su propia suscripción** (`booking-<nombre>`) para no consumir los mensajes de los demás. La crea Broker o cualquiera con la clave del grupo.

Ojo: en Service Bus se publica en el **tópico**, no en una suscripción. Un mensaje de prueba llega como copia a **todas** las suscripciones, incluida `booking`. Antes de publicar, leé la sección 5.

## 3. Qué pasa con cada mensaje

El validador exige los cinco campos del contrato (`solicitudId`, `eventoId`, `asientoId`, `usuarioId`, `fechaSolicitud`), como texto y con valores válidos. Además exige los cuatro metadatos coherentes con el `solicitudId`: `MessageId`, `CorrelationId`, `ContentType = application/json` y `Subject = solicitud-reserva`. Los campos adicionales se ignoran, porque el contrato no los prohíbe.

| Situación | Qué hace Booking |
|---|---|
| El cuerpo no es JSON o no es UTF-8 válido | DLQ con `json_invalido` |
| Falta un campo o un metadato, o tiene un valor inválido | DLQ con `campo_invalido` |
| El mismo `solicitudId` llega con otro evento, asiento o usuario | DLQ con `campo_invalido`. La reserva existente no se toca |
| El asiento está ocupado | Reserva `rechazada` con `asiento_ocupado` y mensaje completado. Es un rechazo de negocio, no va a la DLQ |
| La solicitud ya tiene una reserva confirmada, vencida o rechazada (reentrega) | Se completa el mensaje sin crear otra reserva |
| La reserva queda `retenida` | Ver 3.1 |
| Falla transitoria (base de datos, red) | Se abandona el mensaje y Azure lo vuelve a entregar |
| Otra falla | No se completa. Azure lo vuelve a entregar cuando vence el lock |

### 3.1 Pausa provisional

Mientras no exista el cobro a Payment, una reserva retenida no puede llegar a su resultado final. Por eso, cuando un mensaje deja una reserva `retenida`, el consumidor **no completa el mensaje y deja de procesar hasta que se reinicia la API**. La API sigue respondiendo.

En la práctica:

- Se procesan mensajes hasta encontrar una reserva retenida. Los rechazos y resultados definitivos no pausan el consumidor.
- Al reiniciar, Azure vuelve a entregar el mensaje. Si la reserva sigue retenida, el consumidor se pausa de nuevo. Cuando la reserva vence (paso 8), el mensaje se completa.

Es una medida provisional. Se reemplaza cuando el grupo acuerde en qué momento se completa el mensaje (issue #44) y se integre Payment (#14).

## 4. Logs de auditoría

Siguen `contracts/logs.md` y usan `Shared.Observability`:

| `Operacion` | `TraceId` | `Resultado` y `Motivo` |
|---|---|---|
| `procesar_reserva` | El `solicitudId` del mensaje. Si no se puede leer, un Guid nuevo | `ok` · `rechazada` con `asiento_ocupado`, `json_invalido` o `campo_invalido` · `error` con `pago_pendiente`, `fallo_transitorio`, `fallo_procesamiento` o `cancelada` |
| `consumir_solicitudes` | Guid nuevo para errores de transporte; `solicitudId` al pausar por pago pendiente | `error`, para fallas del processor o la pausa provisional |

La línea de fin de `procesar_reserva` también informa si el mensaje era un **duplicado**.

No se registran cuerpos de mensajes, la connection string ni el texto de las excepciones.

## 5. Cómo probar con Azure

1. **Avisá en el grupo** antes de publicar: el mensaje va a llegar a `booking` y a las suscripciones de los demás.
2. Usá `eventoId` y `asientoId` **inventados**, que no existan en el catálogo de Availability. Así, si una copia sobrevive, nunca ocupa un asiento real.
3. Anotá el `MessageId` de cada mensaje.
4. Publicá primero los mensajes inválidos y el válido al final, porque el válido pausa el consumidor (ver 3.1).
5. Al terminar, conservá evidencia y eliminá únicamente los mensajes identificados de tus pruebas en tu propia suscripción y su DLQ.
6. Las copias de otras suscripciones quedan a cargo de sus responsables. Compartí sus `MessageId` para identificarlas; no las consumas ni elimines sin autorización.

## 6. Qué está verificado

| Prueba | Dónde | Resultado |
|---|---|---|
| Cuerpo que no es JSON | Azure real | DLQ con `json_invalido` y ninguna reserva creada |
| Metadatos incorrectos | Azure real | DLQ con `campo_invalido` y ninguna reserva creada |
| Mensaje válido | Azure real | Reserva `retenida`, `TraceId = solicitudId` y consumidor pausado |
| Repetir la misma solicitud | Local, con PostgreSQL real | Una sola reserva y el mismo ID. No es una reentrega real de Azure |
| Otra solicitud para el mismo asiento | Local, con PostgreSQL real | `rechazada` con `asiento_ocupado` por el índice `ux_reserva_asiento_activo` |
| Arranque con `Enabled = false` | Local | Booking arranca sin credenciales de Azure |

Las pruebas se hicieron con scripts temporales fuera del repo.

**No está verificado:** la carrera entre réplicas, la renovación del lock en operaciones largas ni la recuperación de un cobro.

## 7. Problemas frecuentes

| Síntoma | Causa |
|---|---|
| Booking no arranca y menciona `ServiceBus:SubscriptionName` o `ServiceBus:ConnectionString` | `Enabled` está en `true` y falta uno de esos valores |
| Booking arranca pero no llega ningún mensaje | `Enabled` sigue en `false`, la suscripción es otra, o nadie publicó en el tópico |
| Todos los mensajes van a la DLQ con `campo_invalido` | Falta algún metadato: `MessageId` y `CorrelationId` iguales al `solicitudId`, `ContentType` y `Subject` |
| Aparece "Consumidor pausado: Payment pendiente" | Es lo esperado después de una reserva retenida (ver 3.1). Reiniciar no resuelve la falta de Payment: si sigue retenida, el consumidor vuelve a pausarse. |

## 8. Por qué está hecho así

- **`PeekLock` con confirmación manual:** el mensaje se completa recién después de guardar el resultado. Si Booking se cae en el medio, Azure lo vuelve a entregar y no se pierde la solicitud.
- **Prefetch en 0:** con prefetch, el lock empieza a correr mientras el mensaje espera en memoria, y puede vencer antes de que se procese.
- **Una llamada concurrente por instancia:** la escala la dan las réplicas, que leen de la misma suscripción como consumidores competidores. El índice único impide duplicar reservas activas, pero el manejo controlado de inserciones concurrentes entre réplicas corresponde al issue #17.
- **Inválidos directo a la DLQ:** un mensaje mal formado no se arregla reintentando, así que no tiene sentido gastar entregas.
- El manejo de solicitudes duplicadas que llegan simultáneamente a distintas réplicas sigue pendiente del issue #17.
- **Deshabilitado por defecto:** así cada integrante puede levantar Booking sin tener la clave de Azure.

## 9. Pendientes

| Pendiente | Issue | Responsables |
|---|---|---|
| Acordar cuándo se completa el mensaje de una reserva retenida | #44 | Booking, Payment y Broker |
| Cobro por gRPC y confirmación o rechazo según el pago | #14, #15 | Jonathan y Guillermo |
| Contrato gRPC con Payment | #42 | Jonathan, Guillermo y Axel |
| Carrera entre réplicas con el mismo `solicitudId` | #17 | Guillermo |
