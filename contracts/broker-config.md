# Configuración del broker (Availability y Booking)

Propuesta de configuración de Azure Service Bus para la solicitud de reserva. Es el complemento operativo del contrato del mensaje (`contracts/broker.md`, PR #85): ese documento define el cuerpo y las propiedades del mensaje; este define la topología, los parámetros de la entidad, el comportamiento del consumer y las claves de configuración. No implementa el transporte ni modifica los servicios.

## Topología

| Decisión | Valor | Por qué |
|---|---|---|
| Entidad | Tópico + suscripción | La alternativa, una cola, reparte los mensajes entre las instancias y no permite copias del mismo mensaje para desarrollo. |
| Tópico | `grupo7-booking-topic` | Nombre único del grupo. Availability es su único publicador. |
| Suscripción | `booking` | Booking es su único consumidor. En la demo, las réplicas de Booking consumen de **esta misma suscripción**, como consumidores competidores: una suscripción por réplica multiplicaría las copias. |
| Desarrollo | Suscripción `booking-<nombre>` por integrante, en lugar de `booking` | Para que las copias de Booking en las máquinas de las nueve personas no se roben los mensajes entre ellas. En la demo se vuelve a la suscripción `booking`. |

## Parámetros de la entidad

Valores objetivo: la prueba de publicar y consumir en el namespace confirma que se pueden fijar.

| Parámetro | Valor objetivo | Por qué |
|---|---|---|
| `LockDuration` | 60 s | Presupuesto de procesamiento por intento, antes de que el lock expire y el mensaje se reentregue. |
| `MaxDeliveryCount` | 3 | Tres intentos por mensaje; al agotarlos, el broker lo manda a la DLQ. |
| Sesiones | No se usan | El orden no es una regla del flujo: una sesión por `asientoId` obligaría a consumidores con `RequiresSession` sin necesidad real. |
| Time to live | El default del broker | Si Booking está caído, el mensaje espera en la entidad. No se fija un TTL corto: expirar solicitudes agrega estados que nadie consume. |
| Detección de duplicados | Desactivada | La protección contra reenvíos está en Booking (idempotencia por `solicitudId`, según la parte 2 de `contracts/broker.md`, PR #85); que se active o no queda a confirmar en ese acuerdo. |

## Comportamiento del consumer

| Regla | Valor | Por qué |
|---|---|---|
| Modo de recepción | Peek-lock, confirmación manual | Booking completa el mensaje solo cuando terminó de procesarlo. |
| Prefetch | 0 | Fijo en ambos casos: con prefetch, el mensaje entra en el buffer del cliente y el lock (`LockDuration`) corre desde ese momento, aunque la instancia todavía no lo procese. |
| Completar | Al terminar el procesamiento | El `CompleteMessageAsync` se llama después de persistir el resultado; el punto exacto (retenida o resultado final) lo cierra el acuerdo de `contracts/broker.md` (PR #85), no este documento. |
| Abandonar | Solo ante fallas transitorias | Una base sin respuesta, un corte de red. El abandon devuelve el mensaje a la entidad y el broker lo reentrega. |
| Mensaje inválido | Directo a la DLQ | JSON que no parsea o campos inválidos van a la DLQ sin gastar los tres intentos en un error que no va a cambiar. El agotamiento de las tres entregas (`MaxDeliveryCount = 3`) también manda el mensaje a la DLQ con la razón `MaxDeliveryCountExceeded`, la que el propio broker asigna, verificada en la documentación oficial de Service Bus (Service Bus Dead-Letter Queues). |
| Revisión de la DLQ | Manual | La DLQ no es parte del flujo normal: se revisa a mano; el responsable y el procedimiento a confirmar con el grupo. |

## Claves de configuración

- `ServiceBus:ConnectionString`
- `ServiceBus:TopicName`, con valor por defecto `grupo7-booking-topic`
- `ServiceBus:SubscriptionName`, con valor por defecto `booking`

Ninguna clave se sube al repositorio: cada una se guarda en su máquina, fuera de los archivos versionados, como fija el README.

## Acceso

Availability envía y Booking recibe; ningún otro servicio publica ni consume. Claves separadas por servicio (solo Enviar para el publicador, solo Escuchar para el consumer) solo si la cátedra entrega claves separadas; si no, una cadena común. Qué entregue la cátedra: a confirmar.

## Pendientes

- Namespace de Service Bus para desarrollo y la demo, y la prueba de publicar y consumir que valide los valores objetivo de este documento.
- Si se pueden crear las suscripciones `booking-<nombre>` de desarrollo: a confirmar con lo que entregue la cátedra.
- Valores reales de la entidad (`LockDuration`, `MaxDeliveryCount`, TTL, retención de mensajes): a confirmar con la prueba del namespace.
- Connection strings por máquina (dónde se guardan): a confirmar.
