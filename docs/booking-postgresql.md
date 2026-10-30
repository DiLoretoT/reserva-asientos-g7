# Booking: dominio y PostgreSQL (PR 1)

## Inicio rápido

Requisitos: .NET 10 y Docker Desktop iniciado con contenedores Linux. Desde la raíz:
Si ya se usó la versión con inicialización SQL, leer primero **Bases existentes**.

```powershell
docker compose up -d --wait booking-db
dotnet restore ReservaAsientos.slnx
dotnet build ReservaAsientos.slnx --no-restore
dotnet run --project src/Booking/Booking.Api --launch-profile http --no-build
```

Booking escucha en `http://localhost:5127`; en Development expone `/openapi/v1.json`.
Todavía no tiene endpoints de reservas. Docker crea la base; al iniciar en Development la API ejecuta
`Database.Migrate()` para crear el esquema y aplicar migraciones pendientes. Los siguientes arranques
consultan el historial de EF y no repiten migraciones aplicadas. Fuera de Development no las aplica.

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
  Booking.Data/     BookingDbContext.cs
                    Migrations/*_Inicial.cs, *_Inicial.Designer.cs, BookingDbContextModelSnapshot.cs
  Booking.Api/      Program.cs, BookingJson.cs, appsettings*.json, Properties/
```

Cada capa conserva su proyecto existente. Domain contiene solo datos; `Estado` es obligatorio al
construir una reserva para evitar asumir que toda solicitud se retiene. Business define las opciones;
`Booking:RetentionMinutes = 5` está centralizado en `appsettings.json`, validado como positivo al iniciar.
No calcula vencimientos ni implementa transiciones todavía.

La API registra `BookingDbContext` con `AddDbContext` y `UseNpgsql`: una instancia por ámbito.
El contexto expone `DbSet<Reserva>` y declara todo el mapeo en `OnModelCreating`, sin atributos de EF
ni dependencias de infraestructura en Domain. `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 aporta
el proveedor y Npgsql transitivamente. La API referencia EF Design 10.0.14 con `PrivateAssets=all`.
No se agregan repositorios ni interfaces.

Cada columna tiene un nombre explícito, incluido `Motivo` → `motivo_rechazo`. Los `ValueConverter`
usan una única tabla de equivalencias por enum en ambos sentidos; valores desconocidos fallan
explícitamente. EF conserva `null` para el motivo sin pasarlo al conversor.

Justificación: los dos servicios usan EF Core con migraciones; las reglas de integridad (índice
único parcial, CHECK) están declaradas en el modelo y la base las hace cumplir.

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
| `usuario_id` | TEXT, NOT NULL, exige algún carácter que no sea espacio en blanco; CHECK `length(usuario_id) <= 200` |
| `estado` | TEXT, NOT NULL, CHECK de los cuatro estados |
| `motivo_rechazo` | TEXT nullable, CHECK de los tres motivos |
| `fecha_creacion`, `fecha_actualizacion` | TIMESTAMPTZ, NOT NULL, DEFAULT NOW() |
| `fecha_vencimiento` | TIMESTAMPTZ nullable |

Solo las rechazadas tienen motivo, y deben tenerlo. Retenidas, confirmadas y vencidas requieren
fecha de vencimiento. No hay claves foráneas hacia otros servicios ni tabla de auditoría.
Las fechas leídas son `DateTime` UTC; `FechaVencimiento`, cuando se informa, debe ser UTC.
Se comprobó el INSERT parametrizado anterior con `default(DateTime)` y Kind Unspecified: no falló,
pero almacenó `-infinity` en ambas fechas y no aplicó `NOW()`.

PostgreSQL es el dueño de `fecha_creacion` y `fecha_actualizacion` al insertar. Ambas propiedades usan
`HasDefaultValueSql("NOW()")`, `ValueGeneratedOnAdd` y `BeforeSaveBehavior.Ignore`: EF omite las columnas
en el INSERT incluso si C# trae un valor explícito, y recupera los valores generados por la base.
No hay actualización automática ni trigger. Las modificaciones posteriores deben asignar
explícitamente `fecha_actualizacion`, por ejemplo `SET fecha_actualizacion = CURRENT_TIMESTAMP`.

Se conservan los tipos, nombres, nulabilidad, claves e índices originales. `HasAlternateKey` conserva
la restricción UNIQUE `reservas_solicitud_id_key` y el índice que crea PostgreSQL, sin duplicarlo con
`HasIndex`. La PK conserva el nombre `reservas_pkey`. No se generan identificadores automáticamente.
El único CHECK nuevo es `ck_reservas_usuario_largo`; los otros cinco mantienen nombres y condiciones.

Índices: PK y UNIQUE de solicitud (automáticos); `ux_reserva_asiento_activo` único por
`(evento_id, asiento_id)` para retenidas/confirmadas; `ix_reservas_retenciones_vencimiento` parcial para
buscar retenciones por fecha. Se evaluó un índice adicional de evento: la consulta prevista de
asientos ocupados ya puede usar el índice parcial, que comienza por `evento_id`, al filtrar por
retenidas/confirmadas y luego por vencimiento. No se agrega un índice de evento redundante para
esa consulta, ni otro para solicitud. Si aparecen consultas de historial por evento, reevaluarlo.

La única fuente del esquema es la migración EF `Inicial`; Compose ya no monta scripts de creación.
EF agrega su tabla `__EFMigrationsHistory` para registrar las migraciones aplicadas.
Para generar y revisar futuras migraciones, usar `dotnet-ef` 10.0.14 (instalarlo si no está disponible):

```powershell
dotnet tool install --global dotnet-ef --version 10.0.14
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations add NombreDelCambio --project src/Booking/Booking.Data --startup-project src/Booking/Booking.Api
dotnet ef migrations script --project src/Booking/Booking.Data --startup-project src/Booking/Booking.Api
```

No volver a generar `Inicial`: ya está incluida junto con su snapshot. Las diferencias se corrigen
en el modelo, y EF genera la migración; no se modifica a mano el SQL generado.

### Bases existentes

Una base creada con el SQL anterior no tiene registrada `Inicial`. EF intenta crear `reservas` y
el arranque en Development falla con `42P07` (la relación ya existe). No adopta el esquema ni aplica
automáticamente el nuevo CHECK. Se verificó este caso en una base aislada: la tabla y su fila previa
se conservaron. Reiniciar Docker o usar un script EF idempotente no resuelve la falta de historial.

Para trabajar con una base nueva sin tocar el volumen anterior, elegir un puerto libre y otro
nombre de proyecto Compose, por ejemplo:

```powershell
$env:BOOKING_DB_PORT = '5434'
docker compose -p booking-ef up -d --wait booking-db
$env:ConnectionStrings__Booking = 'Host=localhost;Port=5434;Database=booking_db;Username=booking;Password=test'
dotnet run --project src/Booking/Booking.Api --launch-profile http
```

Esto usa el volumen independiente `booking-ef_booking-data`. Para consultarlo, agregar
`-p booking-ef` a los comandos Compose de abajo. No eliminar ni reinicializar el volumen previo.
Si se necesita migrar sus datos, hace falta una adopción controlada: respaldo, comparación del
esquema completo, revisión de usuarios con más de 200 caracteres, aplicación del CHECK y registro
de la migración solo cuando el esquema coincida. Esa adopción no está automatizada ni se ejecutó
sobre la base existente en esta corrección; no marcar `Inicial` aplicada sin verificar el esquema.

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

Verificado en esta corrección con PostgreSQL 17.11 independiente, usando almacenamiento temporal
y un puerto aleatorio; no se modificó la base habitual:

- API Development sobre base nueva: crea tablas y registra `Inicial`; segundo arranque sin repetirla.
- API Production sobre base vacía: inicia sin crear tablas ni historial.
- Comparación de catálogos con el SQL original: columnas, tipos, orden, nulabilidad, defaults,
  claves, índices y cinco CHECK coinciden. Solo se agrega el CHECK de longitud y el historial EF.
- EF escribe y lee las seis combinaciones válidas de estado/motivo como texto; incluidos
  `pago_fallido` y motivos nulos. Enums o textos desconocidos fallan explícitamente.
- Las fechas generadas son UTC y coinciden con el reloj de PostgreSQL; EF omite ambas en INSERT.
  Un UPDATE normal no cambia la fecha; su asignación explícita sí la modifica.
- Dos retenidas del mismo asiento/evento: `23505`; solicitud duplicada: `23505`.
  Los seis CHECK se verificaron con INSERT reales: 200 caracteres se admiten y 201 se rechazan.
  Las reservas históricas se admiten y pasar una activa a vencida libera el índice.
- Inserciones de verificación revertidas. Base SQL previa: fallo `42P07` comprobado sin pérdida de datos.
- Restore y build de la solución completa: 15 proyectos, 0 errores y 0 advertencias.
  `migrations has-pending-model-changes` no detectó diferencias; Compose es válido.
- La base habitual conservó su contenido (conteo y hash iguales antes/después), sus cinco CHECK
  originales y la ausencia de historial EF; no se aplicaron migraciones sobre ella.

Las comprobaciones de EF se ejecutaron desde un programa temporal externo a la solución, sin
incorporar infraestructura de tests permanente al repositorio.

## Pendiente para PR 2 y revisión humana

Procesamiento del broker e idempotencia, repositorios/transacciones de reserva, vencimientos,
REST conforme a contratos, gRPC/Payment, autenticación y trazabilidad compartida. Acordar también
el transporte de `UsuarioId` en el mensaje: el token entre servicios identifica a Availability.
`contracts/tokens.md` menciona `Shared.Auth`, todavía ausente en esta base de dev. No existen aún
contratos de mensajería, Payment u observabilidad en `contracts/`; revisarlos cuando se integren.

El registro de prompts del integrante está en `prompts/jonathan-villarreal.md`.
No se tocaron los prompts de otros integrantes.
La clasificación prevista es `AI-assisted`, sujeta a la revisión humana antes de futuros commits.

Referencias técnicas: [proveedor Npgsql para EF Core 10](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3),
[valores generados en EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/generated-properties),
[fechas y valores infinitos en Npgsql](https://www.npgsql.org/doc/types/datetime.html).
