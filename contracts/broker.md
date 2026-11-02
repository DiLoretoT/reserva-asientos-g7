# Contrato de mensajes entre Availability y Booking

Propuesta para el [issue #44](https://github.com/DiLoretoT/reserva-asientos-g7/issues/44), pendiente de aprobación de las parejas de Availability, Booking y Broker (Facundo y Gianluca). Define el mensaje de solicitud de un asiento; no implementa el transporte ni modifica los servicios.

El documento tiene dos partes: el **contrato** (cuerpo y propiedades del mensaje), que es lo que se aprueba ahora, y el **comportamiento esperado y los acuerdos pendientes**, que se cierran antes de implementar.

## Responsabilidades y base existente

- **Availability:** valida que el asiento exista en el evento, registra la solicitud y publica sin esperar el resultado de Booking. El adaptador permanece en Gateways, detrás de `IPublicadorSolicitudes`.
- **Azure Service Bus:** acepta y distribuye mensajes del tópico a la suscripción de Booking; no valida el JSON ni decide si un asiento está libre.
- **Booking:** valida el mensaje y delega las reglas a Business, con persistencia en Data mediante EF Core. Decide retención, rechazo y resultado futuro del pago. Availability consulta ese resultado por el [contrato REST](availability-booking.md). No existe mensaje ni cola de respuesta.

Se reutilizan `SolicitudReservaMensaje` y `MapeosBroker` de Availability. Hoy el publicador es simulado y no hay consumidor real. `Booking.Business.SolicitudReserva` recibe los cuatro identificadores; `fechaSolicitud` es información del mensaje, no exige agregar una columna ni reemplaza las fechas o el vencimiento calculados por Booking.

---

# Parte 1: Contrato del mensaje

## Cuerpo del mensaje

JSON UTF-8, propiedades `camelCase`, con los cinco campos existentes, todos obligatorios:

| Campo | Tipo JSON / C# | Significado |
|---|---|---|
| `solicitudId` | string / `Guid` | Identificador único generado por Availability; se conserva en todos los reintentos. |
| `eventoId` | string / `Guid` | Evento al que pertenece el asiento. |
| `asientoId` | string / `Guid` | Asiento solicitado. |
| `usuarioId` | string / `string` | Identificador del usuario autenticado; no es un token. |
| `fechaSolicitud` | string ISO 8601 UTC / `DateTime` | Fecha de registro en Availability, con `Z` final; no es la fecha de vencimiento. |

Availability debe generar `fechaSolicitud` con `DateTime.UtcNow` (`Kind = Utc`) y serializarla en formato ISO 8601 UTC terminado en `Z`. Booking rechaza las fechas sin `Z`, incluso si usan un offset equivalente como `+00:00`; no las convierte ni normaliza.

Ejemplo ilustrativo:

```json
{
  "solicitudId": "c9e2f5a8-1b4d-4c7e-a0f3-8d6b9e2c5a71",
  "eventoId": "3f2b8c1e-6d4a-4b7e-9a21-5c8e0f1d2a34",
  "asientoId": "a1c4e7f0-2b5d-4e8a-9c3f-6d0b1e4a7c92",
  "usuarioId": "4a6cbdb7-0000-4000-8000-000000000001",
  "fechaSolicitud": "2026-11-01T15:30:00Z"
}
```

Booking toma `solicitudId` como TraceId, según [logs y traza](logs.md); no se agrega `traceId` al cuerpo. No viajan tokens, estado de reserva, motivo de rechazo ni datos de pago; los tokens de servicio se rigen por el [contrato de tokens](tokens.md).

## Propiedades propuestas de Service Bus

| Propiedad | Valor propuesto |
|---|---|
| `MessageId` | `solicitudId.ToString("D")`, UUID con guiones en minúsculas. |
| `CorrelationId` | El mismo valor de `MessageId`, para correlacionar la solicitud. |
| `ContentType` | `application/json`. |
| `Subject` | `solicitud-reserva`. Permite a Broker definir la regla de la suscripción de Booking. |
| `ApplicationProperties["version"]` | `1`. Versión del contrato; un cambio incompatible incrementa el valor. |

`MessageId` y `CorrelationId` deben coincidir con el `solicitudId` del cuerpo. Reenviar conserva los cinco campos y estas propiedades; no crea otra solicitud. Son metadatos de transporte, no campos adicionales del JSON. Booking trata una `version` desconocida como mensaje inválido (ver DLQ). Véase [propiedades de mensajes de Azure](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messages-payloads).

---

# Parte 2: Comportamiento esperado y acuerdos pendientes

## Procesamiento esperado (propuesta)

- **Validación:** cuerpo JSON válido, campos presentes y no nulos, UUID válidos y distintos de `Guid.Empty`, `usuarioId` no vacío ni solo espacios y de hasta 200 caracteres (límite existente en Booking), `fechaSolicitud` válida en formato ISO 8601 UTC terminado obligatoriamente en `Z`, `Subject` y `version` esperados y metadatos coherentes. Availability valida la existencia del evento/asiento; Booking no tiene ese catálogo.
- **Duplicados:** asumir posibles reentregas. Usar `solicitudId` como clave de idempotencia; la restricción UNIQUE evita otra reserva, pero no garantiza por sí sola evitar otro cobro. Un duplicado ya terminado conserva su resultado y se completa sin repetir efectos; uno pendiente debe retomar el trabajo, no darse por terminado solo porque existe una reserva. La consulta previa de `RetencionReservas` no resuelve por sí sola duplicados concurrentes ni pagos.
- **Mismo `solicitudId` con datos distintos:** es un mensaje inválido. No sobrescribe la reserva existente; se envía a DLQ con el motivo `conflicto_idempotencia`.
- **Recepción:** proponer `PeekLock` y confirmación manual (`AutoCompleteMessages = false`). Completar con `CompleteMessageAsync` solo después de persistir el resultado o garantizar una continuación durable, según el acuerdo de la sección siguiente.
- **Fallo recuperable:** no completar; permitir reentrega mediante abandono o vencimiento del bloqueo. Si falla la confirmación después de persistir, la reentrega debe reconocer el trabajo realizado. Reintentos limitados, con espera, y renovación del bloqueo mientras se procesa; tiempos y límites por acordar. Estas operaciones siguen la [semántica de recepción y confirmación de Azure](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement).
- **Mensaje inválido:** proponer envío a la DLQ de la suscripción, sin reintentos inútiles ni crear una reserva con datos inválidos. El agotamiento de `MaxDeliveryCount` también deriva a DLQ. Un rechazo de negocio como `asiento_ocupado` es un resultado válido, no un mensaje inválido. La DLQ requiere revisión y recuperación explícitas; no se reprocesa automáticamente ([referencia](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-dead-letter-queues)).
- **Publicación:** esperar la aceptación de Azure antes de informar envío exitoso; eso no significa que Booking haya procesado. Un timeout puede dejar el resultado del envío incierto. Reintentar con el mismo `MessageId`; la [detección de duplicados](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection), si se habilita, depende de su ventana y no reemplaza la idempotencia de Booking.

### Motivos de DLQ propuestos

Lista cerrada para `DeadLetterReason`, con una descripción breve en `DeadLetterErrorDescription`. No deben incluir `usuarioId` ni otros datos personales.

| `DeadLetterReason` | Cuándo se usa |
|---|---|
| `json_invalido` | El cuerpo no es JSON válido o no se puede deserializar. |
| `campo_invalido` | Falta un campo, es nulo, tiene un UUID inválido o `Guid.Empty`, `usuarioId` fuera de regla o fecha sin `Z`. |
| `metadatos_incoherentes` | `MessageId` o `CorrelationId` no coinciden con `solicitudId`, o `ContentType`/`Subject` no son los esperados. |
| `version_no_soportada` | `version` desconocida. |
| `conflicto_idempotencia` | Mismo `solicitudId` con `eventoId`, `asientoId` o `usuarioId` distintos a los ya registrados. |

El agotamiento de `MaxDeliveryCount` o del TTL lo registra Azure con sus propios motivos.

## Acuerdos pendientes antes de implementar

Cada punto se cierra cuando todas las parejas indicadas lo aprueban en el issue.

- [ ] **Contrato y reglas anteriores** (campos, validación, metadatos, `Subject`, `version`, motivos de DLQ y reentrega). Aprueban: Availability, Booking y Broker.
- [ ] **Cuándo completar con Payment.** Se propone completar tras persistir el resultado final (`confirmada`, `rechazada` o `vencida`). Completar al guardar `retenida` solo sería aceptable si existe una continuación durable que retome el pago tras una caída; esa garantía no existe hoy. Acordar recuperación e idempotencia del cobro, y coordinación con el vencimiento: según REST, falta de respuesta de Payment corresponde a `pago_no_disponible`, no simplemente a `vencida`. Aprueban: Booking y Broker, coordinando con Payment y Availability.
- [ ] **Envío incierto.** El flujo simulado actual elimina la solicitud cuando `PublicarAsync` devuelve `false`. Acordar cómo distinguir rechazo de envío incierto antes del publicador real, para no borrar una solicitud que Azure pudo aceptar. Aprueban: Availability y Broker.
- [ ] **DLQ.** Responsable de revisar y reprocesar, y cómo mostrar una solicitud que nunca llegó a procesarse, evitando un "en proceso" indefinido. Aprueban: Broker, Booking y Availability.
- [ ] **Detección de duplicados del tópico.** Decidir si se habilita y con qué ventana. Booking no depende de ella. Aprueban: Broker y Availability.

## Configuración por acordar

Broker debe comunicar el namespace y los nombres reales del tópico y la suscripción; no están definidos en el código revisado. Crear la suscripción antes de publicar y asegurar que su regla entregue las solicitudes con `Subject = solicitud-reserva`. Las réplicas de Booking consumirán de **la misma suscripción**, como consumidores competidores; una por réplica multiplicaría las copias ([tópicos y suscripciones](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-queues-topics-subscriptions)).

Acordar también permisos de envío para Availability y recepción para Booking, mecanismo de autenticación, duración/renovación del bloqueo, `MaxDeliveryCount`, política de reintentos, TTL y tratamiento de expiración hacia DLQ. El TTL del mensaje no es el vencimiento de la reserva. Si se activa detección de duplicados, definir su ventana y compatibilidad con la configuración del tópico. Los valores quedan pendientes de Broker con las parejas consumidoras; conexiones y secretos se proporcionarán fuera del repositorio. No se fijan nombres, credenciales ni tiempos operativos en esta propuesta.
