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
