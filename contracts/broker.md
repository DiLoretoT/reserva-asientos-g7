# Contrato de mensajes entre Availability y Booking

Propuesta para el [issue #44](https://github.com/DiLoretoT/reserva-asientos-g7/issues/44), pendiente de aprobación de las parejas de Availability, Booking y Broker (Facundo y Gianluca). Define el mensaje de solicitud de un asiento; no implementa el transporte ni modifica los servicios.

El alcance de la aprobación es el formato y sus validaciones. Las decisiones operativas pendientes se resolverán durante la implementación y no bloquean la aprobación de este contrato.

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
| `fechaSolicitud` | string ISO 8601 UTC / `DateTime` | Fecha de registro en Availability, con UTC explícito (`Z` o `+00:00`); no es la fecha de vencimiento. |

Se recomienda que Availability genere `fechaSolicitud` con `DateTime.UtcNow` (`Kind = Utc`). Booking acepta fechas ISO 8601 válidas con UTC explícito, terminadas en `Z` o `+00:00`; por ejemplo, `2026-11-01T15:30:00Z` y `2026-11-01T15:30:00+00:00` representan el mismo instante. Rechaza fechas sin zona horaria o con un offset distinto de UTC.

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
| `Subject` | `solicitud-reserva`. Identifica el tipo de mensaje; no exige un filtro en la suscripción. |

`MessageId` y `CorrelationId` deben coincidir con el `solicitudId` del cuerpo. Reenviar conserva los cinco campos y estas propiedades; no crea otra solicitud. Son metadatos de transporte, no campos adicionales del JSON. Véase [propiedades de mensajes de Azure](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-messages-payloads).

---

## Validaciones y motivos propios de DLQ

Booking valida que el cuerpo sea un objeto JSON con los cinco campos presentes, no nulos y de los tipos indicados. Los UUID deben ser válidos y distintos de `Guid.Empty`; `usuarioId` no puede estar vacío ni contener solo espacios y admite hasta 200 caracteres. La fecha debe cumplir la regla de UTC explícito anterior y los cuatro metadatos deben estar presentes y ser coherentes con el contrato. Availability valida la existencia del evento/asiento; Booking no tiene ese catálogo.

Los mensajes inválidos van a DLQ sin crear ni sobrescribir reservas. Solo se usan estos dos motivos propios en `DeadLetterReason`, con una descripción breve en `DeadLetterErrorDescription`, sin datos personales:

| `DeadLetterReason` | Cuándo se usa |
|---|---|
| `json_invalido` | El cuerpo no se puede interpretar como JSON por un error de sintaxis o codificación. |
| `campo_invalido` | El JSON no es un objeto; falta un campo o metadato, es nulo, tiene un tipo o valor inválido; fecha sin zona o sin UTC; metadatos incoherentes; o mismo `solicitudId` con `eventoId`, `asientoId` o `usuarioId` distintos a los registrados. |

Un rechazo de negocio como `asiento_ocupado` no es un mensaje inválido. Los motivos que Azure asigne automáticamente, por ejemplo por entregas agotadas, son independientes de estos dos motivos propios.

# Parte 2: Acuerdos para la implementación

Estos puntos quedan abiertos y **no bloquean la aprobación del contrato**:

- **Confirmación y Payment:** el momento de ejecutar `CompleteMessageAsync` (tras retener o tras el resultado final), la recuperación y la idempotencia del cobro se resolverán durante los issues [#12](https://github.com/DiLoretoT/reserva-asientos-g7/issues/12), [#14](https://github.com/DiLoretoT/reserva-asientos-g7/issues/14) y [#42](https://github.com/DiLoretoT/reserva-asientos-g7/issues/42), coordinando Booking con Payment y Broker. Este documento no elige una alternativa ni incorpora `monto` al mensaje.
- **Duplicados:** `solicitudId` identifica la misma solicitud en las reentregas. Deben evitarse reservas y cobros duplicados; la restricción UNIQUE por sí sola no resuelve el cobro. El tratamiento de una reserva pendiente se definirá junto con el momento de confirmación; no se impone aquí cómo reanudarla.
- **Envío incierto:** Availability y Broker acordarán cómo conservar y reintentar una solicitud si no se conoce el resultado del envío, manteniendo el mismo `MessageId`. Un timeout no demuestra que Azure haya rechazado el mensaje; aceptar el envío tampoco equivale a procesarlo en Booking.
- **DLQ:** Broker, Booking y Availability coordinarán la revisión y recuperación de mensajes y cómo mostrar solicitudes que no llegaron a procesarse.

La topología, filtros, parámetros de entrega, reintentos y claves de configuración se documentan por separado en el [PR #86 de Broker](https://github.com/DiLoretoT/reserva-asientos-g7/pull/86). Este contrato no fija sus valores ni exige filtros de suscripción; no implementa el publicador, consumidor ni la integración con Payment.
