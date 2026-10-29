# PROGRAMACIÓN IV — PR 1: CLASE RESERVA Y CONFIGURACIÓN DE POSTGRESQL

## 1. Rol y objetivo

Actuá como un desarrollador senior especializado en C#, .NET 10, ASP.NET Core, PostgreSQL, Docker, Git, Dapper, Npgsql y arquitectura N-Layer.

Estamos desarrollando el trabajo práctico grupal de Programación IV de la UTN.

Repositorio oficial:

https://github.com/DiLoretoT/reserva-asientos-g7

El archivo `Trabajo_practico.pdf` estará adjunto a esta conversación y contiene el enunciado oficial.

**Tu objetivo es implementar la clase de dominio Reserva y configurar PostgreSQL para Booking**, dejando una base sólida, simple y funcional para desarrollar posteriormente la lógica de reservas.

Quiero que implementes los cambios, los compiles, los pruebes y los documentes, trabajando autónomamente.

Este trabajo constituye la primera etapa de Booking. No quiero implementar todavía todo el microservicio.

### REGLA ABSOLUTA: SOLO TRABAJO LOCAL

El repositorio ya está clonado en esta computadora y Codex está abierto en su carpeta raíz.

**No clones nuevamente el repositorio.**

Podés actualizar tu copia local desde GitHub mediante fetch y pull, siempre que no se pierda trabajo existente.

Tenés autorización para modificar archivos, crear ramas locales, utilizar Docker y ejecutar pruebas.

No tenés autorización para:

- Hacer `git commit`.
- Hacer `git push`.
- Crear Pull Requests.
- Ejecutar merges hacia `dev` o `main`.
- Publicar cambios mediante GitHub CLI o APIs.
- Modificar ramas remotas.
- Borrar trabajo de otros integrantes.
- Eliminar volúmenes existentes de Docker.

Todos los cambios deben permanecer localmente para su revisión humana.

Esta restricción prevalece sobre cualquier instrucción que sugiera publicar cambios automáticamente.

---

## 2. Inspección inicial del repositorio

Antes de escribir código, realizá las siguientes tareas:

1. Ejecutá `git status`.
2. Identificá la rama actual y verificá los remotos.
3. Leé el `README.md` del grupo.
4. Revisá `ReservaAsientos.slnx`.
5. Inspeccioná completamente la estructura de `src/Booking/`.
6. Leé los contratos de `contracts/`.
7. Revisá los cambios existentes relacionados con Availability, Payment, Auth y Shared.
8. Leé el archivo `Trabajo_practico.pdf`, incluyendo sus diagramas y requisitos.

Si el PDF adjunto no puede leerse, informalo claramente antes de avanzar con decisiones que dependan de él. No inventes su contenido.

Utilizá como fuentes principales:

- El enunciado oficial del trabajo práctico.
- El código actual de la rama `dev`.
- Los contratos compartidos del grupo.
- Las convenciones establecidas en `README.md`.

Prestá especial atención a:

- `contracts/availability-booking.md`
- `contracts/tokens.md`
- Los contratos de mensajería, observabilidad y Payment que existan.
- La estructura actual de Booking.Api, Booking.Business, Booking.Data y Booking.Domain.

En caso de contradicciones entre requisitos, informalas y priorizá los contratos vigentes para la integración, sin modificarlos unilateralmente.

## 3. Git: actualizar dev y crear la rama local

El repositorio ya fue clonado desde `dev`.

Verificá que el árbol de trabajo esté limpio o identificá cualquier cambio local existente antes de actualizar.

Después:

1. Ejecutá `git fetch origin`.
2. Cambiá a la rama local `dev`, si no estás ya en ella.
3. Actualizala desde `origin/dev` mediante fast-forward, siempre que sea seguro.
4. Creá una nueva rama local basada en esa versión actualizada.

Nombre exacto:

`feat/clase-reserva-configuracion-postgresql`

Si la rama ya existe, inspeccioná su estado antes de utilizarla.

No sobrescribas cambios locales.

No utilices `git reset --hard`, `git clean -fd`, `git push --force` ni operaciones destructivas equivalentes.

**No hagas commit ni push en ningún momento de esta tarea.**

---

## 4. Requisitos de arquitectura

Booking debe respetar la arquitectura N-Layer y el dominio anémico exigidos en el trabajo práctico.

Utilizá los proyectos existentes:

- `Booking.Domain`
- `Booking.Business`
- `Booking.Data`
- `Booking.Api`

No crees una solución nueva ni estructuras duplicadas.

Las responsabilidades deben estar separadas:

**Booking.Domain:** clases que representan el dominio, sin lógica de persistencia ni dependencias de infraestructura.

**Booking.Business:** lógica y configuración relacionada con las reglas de negocio.

**Booking.Data:** conexión y acceso a PostgreSQL.

**Booking.Api:** configuración y composición de dependencias.

No introduzcas Entity Framework Core.

No modifiques la arquitectura de Availability, Payment, Auth o Shared.

### Simplicidad como prioridad

Quiero una implementación académica sencilla, comprensible y mantenible.

Evitá patrones adicionales que no sean necesarios para cumplir el alcance actual.

No crees múltiples interfaces, clases abstractas, factories, wrappers o servicios auxiliares si no aportan un beneficio concreto.

Respetá la arquitectura requerida, pero evitá sobreingeniería.

### Comentarios en el código

**Reducí los comentarios al mínimo indispensable.**

El profesor solicita justificar las decisiones arquitectónicas y los patrones utilizados, por lo que conservá comentarios breves únicamente cuando expliquen alguna decisión técnica importante o una limitación no evidente.

No agregues comentarios que repitan literalmente lo que hace una propiedad, un método o una instrucción.

No generes bloques extensos de documentación XML para clases simples.

Preferí nombres claros, código legible y documentación breve fuera del código.

---

## 5. Implementación de Booking.Domain

Crear o adaptar las clases necesarias para representar las reservas.

### Clase Reserva

Crear `Reserva.cs` con las siguientes propiedades:

| Propiedad | Tipo C# |
|---|---|
| Id | Guid |
| SolicitudId | Guid |
| EventoId | Guid |
| AsientoId | Guid |
| UsuarioId | string |
| Estado | EstadoReserva |
| Motivo | MotivoRechazo? |
| FechaCreacion | DateTime |
| FechaVencimiento | DateTime? |
| FechaActualizacion | DateTime |

Respetá el dominio anémico.

No agregues operaciones de persistencia ni lógica de transición de estados dentro de esta clase.

No asignes automáticamente el estado Retenida a todas las reservas: algunas podrán rechazarse directamente.

### Enum EstadoReserva

Crear `EstadoReserva.cs`:

```csharp
public enum EstadoReserva
{
    Retenida = 0,
    Confirmada = 1,
    Rechazada = 2,
    Vencida = 3
}
```

### Enum MotivoRechazo

Crear `MotivoRechazo.cs`:

```csharp
public enum MotivoRechazo
{
    AsientoOcupado = 0,
    PagoFallido = 1,
    PagoNoDisponible = 2
}
```

Los valores numéricos son representaciones internas de C#.

**No deben enviarse como números en el contrato REST.**

### Serialización JSON

Respetá el contrato de Availability–Booking.

Los estados deben serializarse como:

- `retenida`
- `confirmada`
- `rechazada`
- `vencida`

Los motivos deben serializarse como:

- `asiento_ocupado`
- `pago_fallido`
- `pago_no_disponible`

Configurar `JsonStringEnumConverter` utilizando `JsonNamingPolicy.SnakeCaseLower`, sin permitir valores numéricos para las conversiones JSON.

Respetar camelCase para los nombres de las propiedades JSON.

No cambiar los contratos compartidos.

Realizar pruebas que demuestren que los enums se serializan y deserializan correctamente.

---

## 6. PostgreSQL

Utilizar PostgreSQL como base de datos exclusiva de Booking.

Nombre de la base:

`booking_db`

La base de datos se ejecutará mediante Docker.

Utilizar Dapper y Npgsql para la conexión desde C#.

**No utilizar Entity Framework Core.**

### Tabla principal: reservas

Crear una migración SQL inicial con el siguiente esquema:

| Columna | Tipo PostgreSQL | Restricciones |
|---|---|---|
| id | UUID | PRIMARY KEY |
| solicitud_id | UUID | NOT NULL, UNIQUE |
| evento_id | UUID | NOT NULL |
| asiento_id | UUID | NOT NULL |
| usuario_id | TEXT | NOT NULL |
| estado | TEXT | NOT NULL, CHECK |
| motivo_rechazo | TEXT | NULL, CHECK |
| fecha_creacion | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |
| fecha_vencimiento | TIMESTAMPTZ | NULL |
| fecha_actualizacion | TIMESTAMPTZ | NOT NULL, DEFAULT NOW() |

### Almacenamiento de los enums

Aunque el dominio utilice enums, PostgreSQL almacenará sus representaciones como texto.

Ejemplos:

- `EstadoReserva.Retenida` → `retenida`
- `EstadoReserva.Confirmada` → `confirmada`
- `MotivoRechazo.PagoFallido` → `pago_fallido`

No guardar los estados como números en PostgreSQL.

Implementar el mapeo necesario entre C# y PostgreSQL dentro de Booking.Data.

Como utilizamos Dapper, verificá expresamente la conversión en ambos sentidos. No supongas que Dapper convierte automáticamente estos valores de texto a enums.

Utilizá una solución sencilla, explícita y centralizada.

No dupliques innecesariamente las reglas de conversión.

### Restricciones de integridad

Implementar restricciones CHECK para garantizar que:

1. Solo existan los cuatro estados permitidos.
2. Solo existan los tres motivos de rechazo permitidos.
3. Una reserva rechazada tenga un motivo válido.
4. Una reserva distinta de rechazada tenga motivo NULL.
5. Una reserva retenida, confirmada o vencida tenga fecha de vencimiento.
6. El identificador de solicitud no se repita.
7. El identificador del usuario no esté vacío ni compuesto solamente por espacios.

No utilizar claves foráneas hacia bases de datos pertenecientes a otros microservicios.

Booking conserva los identificadores de eventos, asientos y usuarios, pero no administra esas entidades.

### Índice único parcial

Crear un índice para impedir dos reservas activas del mismo asiento y evento:

```sql
CREATE UNIQUE INDEX ux_reserva_asiento_activo
ON reservas (evento_id, asiento_id)
WHERE estado IN ('retenida', 'confirmada');
```

Agregar únicamente los índices adicionales que sean necesarios para las consultas previstas.

Como mínimo, evaluar índices para consultas por evento y retenciones pendientes de vencimiento.

La restricción UNIQUE sobre `solicitud_id` ya genera su propio índice.

No crear índices duplicados.

### Limitación conocida

Una reserva retenida que ya superó su fecha de vencimiento seguirá bloqueando el índice mientras su estado permanezca `retenida`.

Esto se resolverá posteriormente mediante transacciones y el proceso de vencimientos.

Documentar esta limitación.

No implementar todavía la lógica completa de concurrencia.

---

## 7. Retención de reservas

La duración de una retención será de:

**5 minutos.**

Preparar una configuración sencilla en Booking.Business.

Por ejemplo:

`Booking:RetentionMinutes = 5`

No repetir este valor manualmente en distintas clases.

Las fechas deben trabajarse en UTC y utilizar `TIMESTAMPTZ` en PostgreSQL.

La lógica posterior de Booking utilizará la hora de PostgreSQL para decidir si una reserva venció.

### No implementar todavía

- BackgroundService.
- Worker de vencimientos.
- Procesamiento periódico automático.
- Transiciones de estado de negocio.
- Confirmaciones de pago.

Todas esas funcionalidades corresponden al PR 2.

---

## 8. Docker: instalación y uso extremadamente sencillos

Esta parte es especialmente importante.

Quiero que cualquier integrante del grupo pueda levantar PostgreSQL con la menor cantidad posible de pasos.

El entorno es **exclusivamente local y de desarrollo académico**.

No necesitamos una configuración de producción.

### Configuración solicitada

- Utilizar PostgreSQL 17 con una imagen oficial y versión explícita.
- Nombre del servicio: `booking-db`.
- Base de datos: `booking_db`.
- Usuario: `booking`.
- Contraseña local de prueba: `test`.
- Puerto local: `5433`, si está disponible.
- Puerto interno PostgreSQL: `5432`.
- Volumen persistente.
- Health check con `pg_isready`.
- Inicialización automática de las tablas.

La contraseña `test` es deliberadamente simple porque el entorno es local, descartable y exclusivamente para desarrollo.

No utilizar esta contraseña ni esta configuración insegura en producción.

No incorporar credenciales reales de otros servicios.

### Configuración mínima

**Quiero que el uso normal no requiera crear manualmente un archivo `.env` ni configurar contraseñas antes de ejecutar Docker.**

Podés utilizar valores predeterminados exclusivos del entorno de desarrollo local.

De ser posible, el comando de arranque debe ser simplemente:

```powershell
docker compose up -d --wait booking-db
```

Ese comando debe:

1. Descargar PostgreSQL si es necesario.
2. Crear el contenedor.
3. Crear `booking_db`.
4. Crear la tabla `reservas`.
5. Crear las restricciones e índices.
6. Iniciar PostgreSQL y comprobar que está saludable.

Utilizá el mecanismo oficial de inicialización de PostgreSQL mediante `/docker-entrypoint-initdb.d/`, si resulta apropiado.

No implementes un sistema complicado de scripts o instaladores para algo que Docker ya puede resolver.

### Integración con Docker existente

Antes de crear archivos, inspeccioná la infraestructura que ya existe en el repositorio.

Si existe Docker Compose, integrá PostgreSQL allí sin romper otros servicios.

No sobrescribas la configuración de Keycloak ni de otros microservicios.

Si ya existe otro contenedor utilizando el puerto 5433, detectá el conflicto e informalo. No detengas ni elimines contenedores ajenos automáticamente.

### Persistencia

Utilizar un volumen nombrado.

Los datos deben conservarse cuando se detenga o reinicie PostgreSQL.

No eliminar volúmenes existentes.

Preparar el SQL inicial como una migración versionada sencilla.

Documentar que los scripts de inicialización solamente se ejecutan cuando la base es nueva.

No agregar todavía Flyway, Liquibase ni otros sistemas complejos de migraciones.

---

## 9. Configuración de Booking.Data

Preparar una conexión funcional mediante Npgsql y Dapper.

La configuración debe ser sencilla y estar centralizada.

Para el desarrollo local, usar:

- Host: `localhost`
- Port: `5433`
- Database: `booking_db`
- Username: `booking`
- Password: `test`

No es necesario solicitar credenciales al usuario para este entorno de prueba.

La configuración de desarrollo puede contener estos valores ficticios siempre que esté claramente identificada como exclusiva de desarrollo y no se trate como una credencial real.

Permitir sobrescribir la conexión mediante variables de entorno para futuras integraciones.

Cuando Booking se ejecute dentro de Docker, deberá utilizar el nombre del servicio `booking-db` en lugar de `localhost`.

### Alcance de Booking.Data

Implementar lo mínimo necesario para:

- Configurar la conexión.
- Abrir conexiones mediante Npgsql.
- Ejecutar una consulta de prueba con Dapper.
- Comprobar la conversión de enums al texto almacenado.
- Dejar preparada la infraestructura para futuros repositorios.

No implementar todavía todos los repositorios de Booking.

No añadir capas de abstracción innecesarias.

---

## 10. Pruebas obligatorias

Quiero que ejecutes pruebas reales y corrijas los errores encontrados.

### A. Compilación

Ejecutar la restauración y compilación de la solución existente:

```powershell
dotnet restore ReservaAsientos.slnx
dotnet build ReservaAsientos.slnx --no-restore
```

Si hay errores originados por otros integrantes, informarlos sin modificar indiscriminadamente otros servicios.

### B. Docker

Validar Compose sin exponer innecesariamente datos de configuración.

Iniciar PostgreSQL y comprobar que el contenedor esté saludable.

### C. Esquema PostgreSQL

Comprobar que:

- Existe `booking_db`.
- Existe `reservas`.
- Las columnas y sus tipos son correctos.
- Las restricciones están creadas.
- Los índices funcionan.

### D. Validaciones SQL

Probar:

1. Insertar una reserva válida.
2. Rechazar un estado inválido.
3. Rechazar un motivo inválido.
4. Rechazar una reserva rechazada sin motivo.
5. Impedir solicitudes duplicadas.
6. Impedir dos reservas activas para el mismo asiento.
7. Permitir reservas históricas rechazadas o vencidas.
8. Liberar el índice al cambiar una reserva a vencida.
9. Rechazar identificadores de usuario vacíos.

### E. Enums

Verificar que:

- C# utiliza enums.
- PostgreSQL guarda texto.
- La lectura desde Dapper reconstruye correctamente los enums.
- JSON utiliza los textos exactos del contrato.
- `pago_fallido` se convierte correctamente en ambos sentidos.
- Los valores numéricos no se aceptan en la entrada JSON.

### F. Persistencia

Comprobar que una reserva persiste después de reiniciar normalmente el contenedor.

No destruir volúmenes existentes.

### G. Limpieza

Las pruebas deben utilizar transacciones reversibles o registros aislados.

Al terminar, no deben quedar datos de prueba innecesarios.

Priorizá pruebas sencillas y reproducibles. Si la solución ya tiene un proyecto de tests, reutilizalo cuando corresponda.

No crees infraestructura compleja únicamente para realizar estas verificaciones.

No declares pruebas exitosas que no se ejecutaron.

---

## 11. Qué NO debe implementarse

Este primer desarrollo debe limitarse al dominio Reserva y la configuración inicial de PostgreSQL.

Quedan expresamente fuera de alcance:

- Endpoints REST completos.
- Procesamiento de solicitudes de reserva.
- Consumo de Azure Service Bus.
- Integración gRPC con Payment.
- Implementación de pagos.
- Autenticación completa con Keycloak.
- BackgroundService de vencimientos.
- Transacciones completas de reserva y pago.
- Tabla de auditoría.
- Logs de auditoría específicos.
- Notificaciones.
- Algoritmos adicionales de coordinación distribuida.

Podés preparar la configuración mínima necesaria para compilar o integrar componentes existentes, sin implementar funcionalidades que correspondan a otros PR.

### Importante sobre auditoría

El TP exige logs de auditoría, pero el grupo también está trabajando con componentes compartidos de observabilidad.

Antes de desarrollar el PR 2 habrá que revisar los contratos de trazabilidad para evitar duplicaciones.

**No crear la tabla de auditoría en este PR.**

---

## 12. Documentación mínima

La documentación debe ser breve y práctica.

Crear un archivo, por ejemplo:

`docs/booking-postgresql.md`

Que explique:

1. Qué se implementó.
2. Estructura de la tabla reservas.
3. Por qué los enums se almacenan como texto.
4. Restricciones e índice único parcial.
5. Retención de 5 minutos.
6. Cómo iniciar Docker.
7. Cómo ejecutar Booking localmente.
8. Cómo ejecutar las pruebas.
9. Qué queda pendiente para el PR 2.

Incluí comandos concretos que otro integrante pueda copiar.

No generes documentación extensa ni varios archivos redundantes.

### Historial de prompts

El repositorio utiliza `prompts/nombre-apellido.md`.

Prepará el registro de esta tarea respetando las convenciones existentes.

Para este nuevo desarrollo quiero que mi archivo de trabajo contenga solamente este prompt maestro, sin inventar prompts anteriores.

No modifiques ni borres archivos de otros integrantes.

Si no podés identificar mi archivo individual, creá provisionalmente `prompts/booking-postgresql-pr1.md` e informalo.

Si ya existe historial personal previo, preservalo sin destruirlo e indicá que debe revisarse antes de la entrega, porque la facultad exige conservar el historial de prompts por integrante.

No inventes información ni afirmes disponer del texto literal del prompt si no podés recuperarlo íntegramente.

---

## 13. Revisión final de Git

Cuando termines de programar y probar:

1. Ejecutá `git status`.
2. Revisá `git diff`.
3. Revisá también todos los archivos nuevos no versionados.
4. Ejecutá `git diff --check`.
5. Comprobá que no modificaste archivos ajenos al alcance.
6. Verificá que no haya credenciales reales.
7. Comprobá que la rama activa sea la correcta.

La rama debe ser:

`feat/clase-reserva-configuracion-postgresql`

### Clasificación de autoría

La clasificación prevista para los futuros commits es:

`AI-assisted`

El integrante definió las decisiones de arquitectura, tecnologías y alcance, y revisará la implementación antes de publicarla.

Utilizaremos esa clasificación cuando se hayan realizado las revisiones humanas y represente fielmente la participación.

**No hagas commits todavía.**

---

## 14. Prohibición de publicación

Esta tarea termina después de las pruebas y la revisión local.

No ejecutar:

- `git add` para preparar el commit final.
- `git commit`.
- `git push`.
- `gh pr create`.
- Cualquier acción equivalente sobre GitHub.
- Merge hacia otras ramas.

No crear Pull Requests.

No publicar la rama.

No modificar el repositorio remoto.

Todos los cambios deben permanecer únicamente en la carpeta local.

Cuando apruebe la implementación, te proporcionaré nuevas instrucciones para preparar commits, publicarlos y crear el PR hacia `dev`.

---

## 15. Informe final obligatorio

Cuando termines, entregá un resumen con:

1. Rama local creada.
2. Commit de `dev` utilizado como base.
3. Archivos creados, modificados y eliminados.
4. Estructura final de Booking.
5. Modelos y enums implementados.
6. Esquema de PostgreSQL.
7. Configuración de Docker.
8. Comandos exactos para iniciar el entorno.
9. Resultados reales de las pruebas.
10. Estado de compilación.
11. Problemas pendientes.
12. Funcionalidades propuestas para PR 2.
13. Confirmación de que no se hicieron commit, push ni PR.

Diferenciá los componentes realmente verificados de los que solamente quedaron preparados.

## 16. Instrucción final

Comenzá inspeccionando el repositorio existente y el archivo `Trabajo_practico.pdf`.

Actualizá localmente `dev` sin modificar GitHub y creá la rama:

`feat/clase-reserva-configuracion-postgresql`

Implementá la clase Reserva, los enums, PostgreSQL, sus restricciones, índices y la conexión con Dapper/Npgsql.

Configurá Docker de manera que el entorno sea fácil de instalar y utilizar por cualquier integrante del grupo, con la contraseña local de prueba `test` y sin pasos manuales innecesarios.

Priorizá la simplicidad, claridad del código, comentarios mínimos, compatibilidad con los contratos grupales y pruebas reales.

Trabajá autónomamente, corregí los errores que encuentres y documentá los resultados.

**Cuando termines, dejá todos los cambios locales, sin commits, sin push y sin Pull Request, para que pueda revisarlos antes de publicarlos.**

Comenzá ahora.