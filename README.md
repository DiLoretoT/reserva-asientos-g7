# Reserva de asientos

Sistema de reserva de asientos para eventos, armado con microservicios en .NET 10. Es el trabajo práctico grupal de Programación IV de la Tecnicatura Universitaria en Programación (UTN), Grupo 7.

## Estructura

```
src/
├── Availability/   sitio MVC y punto de entrada público, con Clean Architecture
├── Booking/        API REST de reservas, con N-Layer
├── Payment/        API gRPC de pagos, con N-Layer
├── Auth/           servidor de autorización
└── Shared/         código común a los servicios, como la configuración de logs
contracts/          contratos entre servicios (archivos .proto de gRPC, endpoints REST y mensajes del broker)
docs/diagramas/     diagrama de arquitectura general y diagramas de clases
prompts/            historial de prompts de cada integrante
```

`ReservaAsientos.slnx` agrupa todos los proyectos para compilarlos juntos. Cada servicio corre como un proceso independiente. Availability publica las solicitudes de reserva en un broker de mensajes (Azure Service Bus), que se las entrega a Booking. Booking llama a Payment por gRPC, y Availability consulta a Booking por REST.

## Booking: desarrollo local

Con Docker Desktop iniciado, ejecutar desde la raíz `docker compose up -d --wait booking-db`.
No requiere `.env`: crea PostgreSQL con credenciales ficticias de desarrollo. Luego ejecutar
`dotnet run --project src/Booking/Booking.Api --launch-profile http`; la API aplica las migraciones
de EF Core en Development. Si la base ya se creó con el SQL anterior, consultar la guía antes de arrancar.
Ver [configuración, verificaciones manuales y alcance de Booking](docs/booking-postgresql.md).

## Cómo trabajamos con Git

### Ramas

- `dev` es la rama por defecto y la de integración. `main` contiene la última versión que el grupo probó con todos los servicios integrados.
- Cada tarea se hace en una rama propia que sale de `dev`, con un nombre del formato `tipo/descripcion`, por ejemplo `feat/listar-asientos` o `docs/tabla-componentes`.

Los tipos de rama y de commit son los mismos:

- `feat` se usa para una funcionalidad nueva.
- `fix` se usa para una corrección.
- `docs` se usa para documentación.
- `refactor` se usa para reorganizar código sin cambiar su comportamiento.
- `test` se usa para pruebas.
- `chore` se usa para configuración y tareas de mantenimiento.

### Pull requests

- Una regla de protección de GitHub bloquea el push directo a `dev` y a `main`, así que todo cambio entra por pull request.
- Cada pull request necesita la revisión favorable de otro integrante. Si el autor sube cambios después, el revisor tiene que aprobar el pull request otra vez.
- `dev` pasa a `main` por pull request cuando la integración funciona.

### Commits

Los commits siguen la convención Conventional Commits, que pide un tipo y una descripción corta en el título. 
Por ejemplo:

```
feat: agrega el listado de asientos de un evento

Explicación opcional de qué cambia y por qué.

AI-assisted
```

- El cuerpo es opcional y explica el motivo del cambio.
- La última línea indica cuánto intervino la IA. `AI-full` indica que la IA escribió el código y nadie lo modificó. `AI-assisted` indica que la IA participó y una persona revisó o ajustó el resultado. `Manual` indica que el código se escribió sin IA.

## Prompts

Cada integrante guarda en `prompts/nombre-apellido.md` el historial de instrucciones que le dio a la IA.

## Secretos

El connection string del broker y cualquier otra clave no se suben al repositorio. Cada uno los guarda en su máquina, fuera de los archivos versionados.

