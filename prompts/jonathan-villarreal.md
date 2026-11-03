## PR 1 — Modelo de Reserva y PostgreSQL

Estamos desarrollando un trabajo práctico grupal de Programación IV sobre un sistema distribuido de reserva de asientos.

Repositorio: https://github.com/DiLoretoT/reserva-asientos-g7

Te adjunto `Trabajo_practico.pdf` con los requisitos del proyecto.

Necesito implementar la estructura inicial del microservicio Booking, respetando la arquitectura N-Layer y el dominio anémico definidos en el trabajo práctico. Revisá primero el repositorio, los contratos existentes y la implementación de Availability para mantener consistencia con el grupo.

El desarrollo debe incluir:

- La clase `Reserva` y los enums `EstadoReserva` y `MotivoRechazo`, respetando los contratos REST.
- PostgreSQL 17 como base de datos, utilizando Entity Framework Core con Npgsql, igual que Availability.
- Una tabla `reservas` con los campos, restricciones e índices necesarios para evitar solicitudes duplicadas y reservas activas del mismo asiento.
- Migraciones de EF Core aplicadas automáticamente al iniciar la API en Development.
- Una retención configurable de cinco minutos.
- Configuración de Docker sencilla, con credenciales de prueba para desarrollo local, de manera que cualquier integrante pueda iniciar la base fácilmente.

Mantené el código simple, ordenado y con comentarios mínimos. No agregues patrones ni abstracciones innecesarias.

Por ahora no implementes endpoints, comunicación con el broker, pagos, auditoría ni procesos automáticos de vencimiento. Eso se desarrollará posteriormente.

Trabajá sobre el clon existente de `dev` y creá la rama `feat/clase-reserva-configuracion-postgresql`.

Implementá los cambios, verificá que la solución compile y comprobá que PostgreSQL funcione correctamente. Actualizá brevemente la documentación con los pasos de instalación y ejecución.

**Dejá todos los cambios locales, sin commit, push ni Pull Request, para revisarlos antes de publicarlos.**

## Booking — BackgroundService de vencimiento de reservas

Quiero implementar el vencimiento automático de reservas en Booking, respetando la arquitectura N-Layer y utilizando EF Core con PostgreSQL.

La rama `dev` ya está actualizada. Creá una nueva rama local llamada `feat/vencimiento-automatico-reservas`.

Revisá primero las clases existentes de Booking para reutilizar la estructura y no modificar el trabajo de mis compañeros.

### Implementación

1. Crear un `BackgroundService` que se ejecute automáticamente cada 15 segundos, con ese intervalo configurable.
2. Buscar las reservas en estado `retenida` cuya `FechaVencimiento` sea menor o igual a la hora actual de PostgreSQL.
3. Actualizar esas reservas a `vencida` mediante EF Core, de manera eficiente y segura ante ejecuciones simultáneas.
4. Actualizar `FechaActualizacion` utilizando también la hora de PostgreSQL.
5. Garantizar que, al cambiar el estado, el asiento quede liberado según el índice único parcial existente.

El proceso debe ejecutarse mientras Booking esté funcionando, sin depender de solicitudes HTTP. Utilizá correctamente los ámbitos de dependencias de EF Core y evitá agregar clases o abstracciones innecesarias.

### Alcance

- No implementar logs, Serilog, Seq ni auditoría en este PR.
- No agregar endpoints, tablas ni migraciones si no son necesarias.
- No modificar la lógica de retención de mi compañero.
- Mantener el código simple, con comentarios mínimos.

Verificá que la solución compile y probá el vencimiento con PostgreSQL, comprobando que las reservas confirmadas o no vencidas permanezcan intactas y que ejecutar el proceso varias veces no genere problemas. Utilizá datos de prueba aislados, sin afectar las reservas existentes. No incorpores archivos de pruebas permanentes.

Agregá este pedido al final de `prompts/jonathan-villarreal.md`, conservando el historial anterior, y documentá brevemente cómo funciona el proceso.

**Importante:** dejá todos los cambios locales para revisión. No hagas commit, push, merge ni Pull Request.

Al terminar, entregame un resumen de los cambios y las verificaciones realizadas.

## Booking — Contrato de mensajes del broker (issue #44)

Quiero preparar el contrato de mensajes entre Availability y Booking mediante Azure Service Bus, respetando la arquitectura y las convenciones existentes del grupo.

Primero verificá el estado local, actualizá `dev` desde `origin/dev` mediante fast-forward y creá una rama nueva `docs/contrato-broker`. Si hay cambios locales sin guardar o algún conflicto, detenete y avisame sin descartarlos.

### Trabajo

1. Revisá el issue #44, los contratos existentes y las clases actuales de Availability y Booking relacionadas con solicitudes y mensajes.
2. Creá `contracts/broker.md` con una propuesta clara y concisa que incluya:
   - Responsabilidades de Availability, Azure Service Bus y Booking.
   - Formato JSON `camelCase`, tipos y ejemplo de mensaje, reutilizando los cinco campos existentes.
   - `solicitudId` como identificador único y TraceId, sin agregar otro campo de trazabilidad al cuerpo.
   - Propuesta para `MessageId`, `CorrelationId` y `ContentType` de Azure Service Bus.
   - Reglas esperadas para mensajes duplicados, validación, confirmación de procesamiento, reintentos y mensajes inválidos.
   - Configuración necesaria de tópico y suscripción, sin inventar valores ni incluir credenciales.
3. Marcá claramente las decisiones que requieran aprobación de las parejas de Availability, Booking o Broker, especialmente cuándo se considera completado un mensaje teniendo en cuenta la futura integración con Payment.

### Alcance

- Este trabajo es exclusivamente de documentación del contrato.
- No implementar todavía el publicador ni el consumidor real.
- No modificar clases, APIs, bases de datos ni configuración de los servicios.
- No agregar logs, auditoría ni funcionalidades nuevas.
- Evitar documentación extensa o decisiones innecesarias.

Agregá este pedido al final de `prompts/jonathan-villarreal.md`, conservando el historial anterior.

Dejá únicamente esos dos archivos modificados, listos para revisión. No hagas commit, push ni Pull Request.

Al terminar, entregame un resumen de la propuesta, las decisiones pendientes de acordar y los archivos modificados.

## Finalizar PR #85 — Contrato del broker

Estamos trabajando en el repositorio `reserva-asientos-g7`. Quiero incorporar las observaciones de Facundo al contrato del broker (PR #85, issue #44).

Ya acordamos con los compañeros que **el momento de completar los mensajes se definirá durante la implementación**, coordinando Booking con Payment. No debe bloquear la aprobación del contrato.

### Preparación

1. Trabajá sobre la rama existente `docs/contrato-broker`.
2. Verificá el estado de Git antes de modificar archivos. No descartes cambios locales.
3. Revisá el comentario de Facundo en el PR #85 y el contenido actual de `contracts/broker.md`.
4. No crees otra rama ni otro PR.

### Cambios en contracts/broker.md

1. **Mantener el cuerpo JSON de cinco campos:** `solicitudId`, `eventoId`, `asientoId`, `usuarioId` y `fechaSolicitud`, sin agregar otros.
2. **Mantener los metadatos de Service Bus:** `MessageId`, `CorrelationId`, `ContentType = application/json` y `Subject = solicitud-reserva`.
3. **Eliminar `ApplicationProperties["version"]`:** quitar también las validaciones, motivos de DLQ y referencias que dependan de esa propiedad.
4. **Simplificar los motivos propios de DLQ** a `json_invalido` y `campo_invalido`. Clasificar de manera coherente los errores de campos, metadatos y conflictos de idempotencia, sin introducir nuevas categorías.
5. **Flexibilizar el formato de `fechaSolicitud`:** exigir una fecha ISO 8601 con UTC explícito, aceptando tanto `Z` como `+00:00`. Mantener `DateTime.UtcNow` como recomendación para Availability. Rechazar fechas sin información de zona horaria.
6. **Separar contrato de implementación:** mantener el documento centrado en el formato del mensaje y sus validaciones. Simplificar las secciones operativas para evitar duplicar la configuración que Facundo está documentando en el PR #86.
7. **Dejar pendiente la confirmación del mensaje:** no decidir si `CompleteMessageAsync` se ejecuta después de la retención o del resultado final. Indicar brevemente que esta decisión, junto con la recuperación e idempotencia del cobro, se resolverá durante los issues #12, #14 y #42, en coordinación con Booking y Payment.
8. Mantener los acuerdos pendientes que sigan siendo relevantes, sin convertir decisiones de implementación en requisitos obligatorios del contrato.

### Alcance

- Modificar únicamente `contracts/broker.md` y `prompts/jonathan-villarreal.md`.
- Agregar este pedido al final del historial de prompts sin modificar las entradas anteriores.
- No modificar código, configuración, bases de datos ni migraciones.
- No implementar el publicador, consumidor ni integración gRPC.
- Mantener la documentación clara, breve y sin contradicciones.

### Publicación

1. Revisá que los cambios respondan a los comentarios de Facundo y que el ejemplo JSON siga siendo válido.
2. Verificá el diff y comprobá que no existan cambios adicionales.
3. Creá un commit con un mensaje descriptivo terminado en `AI-assisted`.
4. Hacé push a la misma rama para actualizar el PR #85 existente.
5. Actualizá la descripción del PR para reflejar la eliminación de `version`, la nueva validación de fechas, los dos motivos de DLQ y la separación de las decisiones operativas.

**No hagas merge, no apruebes el PR ni abras otro.**

Al finalizar, informame los archivos modificados, el commit realizado, el estado del PR y cualquier decisión que haya quedado pendiente de coordinación.


---

## 2026-11-03 — Consumidor de Azure Service Bus en Booking (issue #12)

Estoy con el issue #12: el consumidor de Azure Service Bus en Booking, preparado para integrar después el cobro a Payment (#14 y #15). Trabajá desde el `dev` más reciente en la rama `feat/consumidor-servicebus-booking`, sin descartar cambios locales. Antes de escribir nada, leé `contracts/broker.md`, la configuración del broker del PR #86 y el código actual de Booking. No toques Availability, Payment ni Shared.Auth.

Quiero un `BackgroundService` con `ServiceBusProcessor` que escuche `grupo7-booking-topic` y la suscripción de `ServiceBus:SubscriptionName`, con `PeekLock`, sin autocompletar, prefetch 0 y una llamada concurrente. La conexión va en `ServiceBus:ConnectionString`, nunca en archivos versionados, y el consumidor tiene que venir deshabilitado por defecto.

El validador revisa los cinco campos y los metadatos del contrato, y acepta fechas UTC con `Z` o `+00:00`. Los mensajes inválidos van a la DLQ solo con `json_invalido` o `campo_invalido`; un rechazo por `asiento_ocupado` no va a la DLQ, y ante una falla transitoria el mensaje se tiene que poder volver a entregar. Reutilizá `SolicitudReserva` y `RetencionReservas`, con un scope por mensaje, y usá el `solicitudId` como TraceId. No fuerces un máximo de entregas: en Azure es 10.

Todavía no está acordado cuándo completar el mensaje de una reserva retenida, así que no lo completes mientras falte el cobro. No toques la concurrencia de Guillermo (#17); si ves un problema, documentalo.

Agregá pruebas sin credenciales reales, compilá y verificá que Booking siga andando sin Service Bus. Actualizá mi historial de prompts. No hagas commit ni push.

## 2026-11-03 — Conexión real con Azure

Seguimos en la misma rama. Quiero verificar la conexión real con Azure usando la connection string de User Secrets, sin mostrarla ni copiarla al código, la documentación o los logs. La clave se renovó: usá la nueva.

Prepará la suscripción de desarrollo `booking-jonathan` sin tocar la compartida `booking`. Probá recepción, validación, reentregas y DLQ sin publicar en el tópico compartido si eso afecta a los demás; si necesitás permisos o coordinación, avisame.

Revisá el manejo de excepciones y la pausa provisional del consumidor, y corregí solo lo que sea del #12. Cuando termines, sacá `tests/Booking.Tests` y restaurá `ReservaAsientos.slnx`, como acordamos para este PR. Compilá, revisá el diff y documentá resultados y pendientes. No hagas commit ni push.

## 2026-11-03 — Duplicados e índice único con PostgreSQL real

Sin rehacer el consumidor, quiero probar con PostgreSQL real dos casos: que procesar dos veces la misma solicitud no cree otra reserva (aclarando que no es una reentrega real de Azure) y que dos solicitudes distintas para el mismo asiento terminen con la segunda rechazada por `asiento_ocupado` gracias al índice único parcial. Para esto no uses EF Core InMemory.

No publiques en el tópico compartido porque no está coordinado con el grupo. Después de las pruebas, sacá `tests/Booking.Tests`, restaurá la solución, compilá y documentá. No cambies la pausa ni el completado del mensaje, que siguen pendientes de acordar. No hagas commit ni push.

## 2026-11-03 — Pruebas con Azure real y revisión final

Ahora sí hacé las pruebas con Azure, en este orden, sobre `booking-jonathan`: un cuerpo que no sea JSON tiene que ir a la DLQ con `json_invalido`; uno con metadatos incorrectos, con `campo_invalido`; y uno válido tiene que crear una reserva `Retenida` en un PostgreSQL temporal, con el TraceId correcto y el consumidor pausado. Usá IDs inventados que no existan en el catálogo de Availability. Al terminar, limpiá solo nuestros mensajes de `booking-jonathan` y su DLQ; las copias de las otras suscripciones quedan a cargo de sus responsables.

Revisá también el validador: el contrato pide cinco campos obligatorios pero no prohíbe otros, así que ajustalo para que ignore los campos de más sin aflojar los obligatorios. No cambies el completado del mensaje, que sigue pendiente del grupo (#44), ni `RetencionReservas`.

Dejá `docs/booking-servicebus.md` como documentación técnica, no como bitácora, con lo verificado y lo pendiente (#14, #15, #17 y #44). No vuelvas a agregar `Booking.Tests` y dejá los scripts de prueba fuera del repo. Compilá, revisá que no haya credenciales y registrá este prompt. No hagas commit ni push.


## 2026-11-03 — Publicación del consumidor de Booking (issue #12)

Vamos a publicar el trabajo del issue #12: consumidor de Azure Service Bus en Booking.

Estamos en la rama `feat/consumidor-servicebus-booking`.

Antes de publicar:

1. Hacé `git fetch origin` y verificá que la rama esté actualizada respecto de `origin/dev`. Si hubo cambios nuevos, integrálos sin perder nuestro trabajo ni sobrescribir cambios de compañeros.
2. Revisá el diff completo. Confirmá que solo incluya archivos correspondientes al consumidor de Booking, su documentación y mi historial de prompts.
3. Verificá que no haya credenciales, archivos temporales, proyectos de pruebas ni modificaciones ajenas al issue.
4. Comprobá que `ServiceBus:Enabled` siga en `false` por defecto y que `ReservaAsientos.slnx` conserve sus proyectos originales.
5. Ejecutá restore y build de toda la solución. Si encontrás errores o conflictos, resolvelos antes de publicar, respetando los cambios del equipo.

Si todo está correcto, realizá el commit y push en nuestra rama, respetando la convención del repositorio e incluyendo `AI-assisted` al final del mensaje de commit.

Después creá un PR hacia `dev`, asociado al issue #12, con un título claro y una descripción breve que incluya:

- Qué implementamos: consumidor Service Bus, validación del contrato, DLQ, reintentos, retención y trazabilidad.
- Verificaciones: tres pruebas reales con Azure, duplicados y asiento ocupado con PostgreSQL real, y compilación completa.
- Limitaciones: consumo deshabilitado por defecto y pausa provisional para reservas retenidas.
- Pendientes: integración con Payment (#14/#15), concurrencia de Guillermo (#17) y acuerdo sobre `CompleteMessageAsync` (#44).

No presentes esas dependencias como funcionalidades terminadas. No incluyas secretos ni identificadores sensibles en la descripción.
