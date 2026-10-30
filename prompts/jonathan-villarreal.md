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