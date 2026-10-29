# Booking: dominio y PostgreSQL (PR 1)

Base: `dev` en `0d7808a45882ef8b8c27f4fda653a6fa2fc814b8`.
Rama local: `feat/clase-reserva-configuracion-postgresql`. Cambios sin commit ni publicación.

## Inicio rápido

Requisitos: .NET 10 y Docker Desktop iniciado con contenedores Linux. Desde la raíz:

```powershell
docker compose up -d --wait booking-db
dotnet restore ReservaAsientos.slnx
dotnet build ReservaAsientos.slnx --no-restore
dotnet run --project src/Booking/Booking.Api --launch-profile http --no-build
```

Booking escucha en `http://localhost:5127`; en Development expone `/openapi/v1.json`.
Todavía no tiene endpoints de reservas. La API no aplica migraciones al arrancar.

El Compose raíz agrega únicamente `booking-db`; Keycloak conserva su Compose independiente en
`src/Auth/keycloak/`, sin cambios ni necesidad de su `.env` para iniciar PostgreSQL.

| Configuración local | Valor |
|---|---|
| Imagen oficial | `postgres:17.11` |
| Base / usuario / contraseña | `booking_db` / `booking` / `test` |
| Host / puertos | `localhost:5433` desde Windows; `booking-db:5432` desde la misma red Docker |
| Volumen | `booking-data` (Compose antepone el nombre del proyecto) |
| Salud | `pg_isready` por TCP; `up --wait` espera a PostgreSQL |

`test` es una contraseña ficticia exclusiva de desarrollo académico. No usar esta configuración en
producción. El puerto se publica solo en `127.0.0.1`. No se necesitan archivos `.env`.

La conexión local está únicamente en `appsettings.Development.json`. Puede sobrescribirse:

```powershell
$env:ConnectionStrings__Booking = 'Host=localhost;Port=5433;Database=booking_db;Username=booking;Password=test'
```

En una futura API dentro de la misma red Docker, usar `Host=booking-db;Port=5432`.
Fuera de Development hay que proporcionar `ConnectionStrings__Booking`; no hay credenciales por defecto.
Si 5433 está ocupado, no detener el servicio ajeno: elegir un puerto libre y ajustar ambos valores:

```powershell
$env:BOOKING_DB_PORT = '5434'
$env:ConnectionStrings__Booking = 'Host=localhost;Port=5434;Database=booking_db;Username=booking;Password=test'
docker compose up -d --wait booking-db
```

Para detener sin perder datos: `docker compose stop booking-db`. Para reiniciar:
`docker compose restart booking-db`. No eliminar el volumen.

## Capas y mapeo

```text
src/Booking/
  Booking.Domain/    Reserva.cs, EstadoReserva.cs, MotivoRechazo.cs
  Booking.Business/ BookingOptions.cs
  Booking.Data/     BookingDatabase.cs, ReservaRegistro.cs
                    Migrations/001_crear_reservas.sql
  Booking.Api/      Program.cs, BookingJson.cs, appsettings*.json, Properties/
```

Cada capa conserva su proyecto existente. Domain contiene solo datos; `Estado` es obligatorio al
construir una reserva para evitar asumir que toda solicitud se retiene. Business define las opciones;
`Booking:RetentionMinutes = 5` está centralizado en `appsettings.json`, validado como positivo al iniciar.
No calcula vencimientos ni implementa transiciones todavía.

La API compone un `NpgsqlDataSource` singleton (pool compartido, dispuesto por DI) y `BookingDatabase`.
Cada operación abre y dispone su conexión; `ObtenerHoraUtcAsync` ejecuta Dapper contra PostgreSQL.
No hay EF Core, repositorios completos ni interfaces adicionales en Booking.

`ReservaRegistro.DesdeDominio` convierte los enums a texto antes de parametrizar Dapper.
Para leer, usar `SELECT {ReservaRegistro.ColumnasSql} ...`, materializar `ReservaRegistro` y llamar
`ADominio()`. Los alias resuelven snake_case, incluido `motivo_rechazo` → `Motivo`, sin configuración
global de Dapper. Cada enum tiene una única tabla de equivalencias usada en ambos sentidos;
los valores desconocidos fallan explícitamente. No consultar directamente `Query<Reserva>`.

Se guardan textos legibles y compatibles con el contrato: `retenida`, `confirmada`, `rechazada`,
`vencida`, `asiento_ocupado`, `pago_fallido`, `pago_no_disponible`. La API configura camelCase y
`JsonStringEnumConverter(SnakeCaseLower, allowIntegerValues: false)` tanto para MVC como para Minimal APIs.
Los números JSON, incluso números entre comillas, se rechazan.

## Esquema e inicialización

| Columna | Tipo y restricciones |
|---|---|
| `id` | UUID, PK |
| `solicitud_id` | UUID, NOT NULL, UNIQUE |
| `evento_id`, `asiento_id` | UUID, NOT NULL |
| `usuario_id` | TEXT, NOT NULL, exige algún carácter que no sea espacio en blanco |
| `estado` | TEXT, NOT NULL, CHECK de los cuatro estados |
| `motivo_rechazo` | TEXT nullable, CHECK de los tres motivos |
| `fecha_creacion`, `fecha_actualizacion` | TIMESTAMPTZ, NOT NULL, DEFAULT NOW() |
| `fecha_vencimiento` | TIMESTAMPTZ nullable |

Solo las rechazadas tienen motivo, y deben tenerlo. Retenidas, confirmadas y vencidas requieren
fecha de vencimiento. No hay claves foráneas hacia otros servicios ni tabla de auditoría.
Las fechas C# deben ser UTC; Npgsql devuelve TIMESTAMPTZ como `DateTime` UTC.
El default de `fecha_actualizacion` sirve al insertar: las futuras actualizaciones deberán asignarlo
explícitamente usando la hora de PostgreSQL.

Índices: PK y UNIQUE de solicitud (automáticos); `ux_reserva_asiento_activo` único por
`(evento_id, asiento_id)` para retenidas/confirmadas; `ix_reservas_retenciones_vencimiento` parcial para
buscar retenciones por fecha. Se evaluó un índice adicional de evento: la consulta prevista de
asientos ocupados ya puede usar el índice parcial, que comienza por `evento_id`, al filtrar por
retenidas/confirmadas y luego por vencimiento. No se agrega un índice de evento redundante para
esa consulta, ni otro para solicitud. Si aparecen consultas de historial por evento, reevaluarlo.

Docker monta la migración versionada en `/docker-entrypoint-initdb.d/`: se ejecuta automáticamente
**solo cuando el volumen está vacío**. Reiniciar no vuelve a ejecutar ni actualizar scripts.
Para una base existente sin esta tabla, aplicar una única vez, sin borrar datos:

```powershell
Get-Content -Raw src/Booking/Booking.Data/Migrations/001_crear_reservas.sql |
    docker compose exec -T booking-db psql -U booking -d booking_db -v ON_ERROR_STOP=1
```

La migración es transaccional y no es idempotente: si la tabla ya existe, falla sin reemplazarla.
Los cambios posteriores deben ir en nuevas migraciones, aplicadas explícitamente a volúmenes existentes.

**Limitación:** una retención vencida por hora sigue ocupando el índice mientras conserve estado
`retenida`. El contrato REST exige informar el asiento libre/estado vencido desde esa hora; PR 2 debe
resolver las consultas y la liberación transaccional usando el reloj de PostgreSQL. Este PR no implementa
el worker, las consultas REST ni la lógica completa de concurrencia.

## Verificaciones manuales

```powershell
docker compose config --quiet
docker compose up -d --wait booking-db
docker compose ps booking-db
docker compose exec -T booking-db psql -U booking -d booking_db -c '\d reservas'
docker compose exec -T booking-db psql -U booking -d booking_db -c 'SELECT * FROM reservas LIMIT 10;'
```

Estos comandos muestran la salud del contenedor, la estructura de la tabla y hasta diez reservas,
sin modificar datos. Para compilar, usar los comandos de restore y build del inicio rápido.
El PR 1 no incluye un proyecto de pruebas automatizadas ni un script de prueba de persistencia.

Verificado tras la limpieza: restore y build de los 15 proyectos correctos, con 0 errores y
0 advertencias; Compose válido, PostgreSQL saludable y tabla `reservas` existente.

## Pendiente para PR 2 y revisión humana

Procesamiento del broker e idempotencia, repositorios/transacciones de reserva, vencimientos,
REST conforme a contratos, gRPC/Payment, autenticación y trazabilidad compartida. Acordar también
el transporte de `UsuarioId` en el mensaje: el token entre servicios identifica a Availability.
`contracts/tokens.md` menciona `Shared.Auth`, todavía ausente en esta base de dev. No existen aún
contratos de mensajería, Payment u observabilidad en `contracts/`; revisarlos cuando se integren.

El historial del integrante está en `prompts/jonathan-villarreal.md` y se conserva sin cambios durante
esta limpieza. No se tocaron los archivos de prompts de otros integrantes.
La clasificación prevista es `AI-assisted`, sujeta a la revisión humana antes de futuros commits.

Referencias técnicas: [Npgsql y su pool de conexiones](https://www.npgsql.org/doc/basic-usage.html),
[inicialización de la imagen oficial de PostgreSQL](https://hub.docker.com/_/postgres).
