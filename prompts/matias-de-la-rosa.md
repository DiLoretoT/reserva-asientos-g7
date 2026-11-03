# Prompts de Matías De La Rosa

Historial de prompts usados para implementar el TP, en orden cronológico.

## 2026-10-27 11:09
Vamos a trabajar en el issue #8 de Availability: base de datos propia de
eventos, asientos y solicitudes. Availability sigue Clean Architecture con
dominio anémico, y en Entities ya existen Evento y Asiento.

Antes de escribir código, proponé un plan: qué clases van en cada proyecto
(Entities, UseCases, Gateways, Web), qué motor de base conviene para que
funcione igual en la PC de todos los integrantes, y cómo se respeta la regla
de dependencias. No crees archivos todavía.
Por qué: pedir el plan primero te deja corregir el rumbo antes de que haya código. Y en la defensa podés explicar que lo pensaste antes de implementarlo.

## 2026-10-27 11:55
Usemos SQLite. Creá la rama feat/base-datos-availability desde dev actualizado.

En Availability.Entities/Solicitudes creá la entidad SolicitudReserva, anémica,
con el mismo estilo que Evento.cs: Id Guid generado por la aplicación,
EventoId, AsientoId, UsuarioId y FechaSolicitud en UTC. No debe tener estado
de reserva porque el dueño del estado es Booking.

Justificá en comentarios cada decisión. Compilá y mostrame el código.
No hagas commit.

## 2026-10-27 18:01
En Availability.UseCases creá los puertos de salida IEventoRepository
(listar eventos y obtener uno con sus asientos) e ISolicitudRepository
(agregar y obtener por Id). Deben recibir y devolver entidades de dominio,
nunca entidades de persistencia.

Organizalos en carpetas por entidad, como en Entities. Justificá en
comentarios el patrón Ports & Adapters, el patrón Repository y por qué las
interfaces viven en UseCases y no en Gateways. Compilá y mostrame el código.

## 2026-10-28 09:15
En Availability.Gateways implementá la persistencia con Entity Framework Core
y SQLite:

- Entidades de persistencia separadas de las de dominio, como en el ejemplo
  de la cátedra (Agenda.Gateways), con los atributos de base y las claves
  foráneas.
- AvailabilityDbContext con: índice único por evento, fila y número; borrado
  en cascada de asientos; borrado restringido de solicitudes; fechas leídas
  como UTC.
- Datos iniciales de 2 o 3 eventos con Guid fijos y legibles, para que todo
  el grupo use los mismos IDs.
- Mapeos manuales entre persistencia y dominio, sin AutoMapper.
- Repositorios que implementen los puertos.
- Un método de extensión AddGateways para que Program.cs agregue una sola línea.

Justificá en comentarios: Database per Service, la elección de SQLite aunque
el diagrama dice SQL Database, y cada regla del DbContext. Compilá.

## 2026-10-28 09:45
Registrá AddGateways en Program.cs y el connection string en appsettings.json.
Agregá *.db al .gitignore. Generá la migración inicial en
Gateways/Persistencia/Migraciones y hacé que se aplique sola al arrancar en
Development. Ejecutá el sitio y confirmá que se crea la base con los datos
iniciales.

## 2026-10-28 13:00
Probá los repositorios contra una copia de la base: listar eventos, obtener
uno con sus asientos, que un evento inexistente devuelva null, guardar y leer
una solicitud, y que la base rechace una solicitud con un asiento inexistente.

Después revisá que Entities y UseCases no dependan de Entity Framework.
Mostrame el resumen de los cambios y proponé un mensaje de commit con
Conventional Commits. No hagas commit.

## 2026-10-31

Vamos a trabajar en el issue #4 de Availability: solicitud de reserva y
publicación en el broker (paso 3 del enunciado).

Antes de escribir código, proponé un plan de archivos siguiendo el mismo
patrón que usó Tomás en ListarAsientosDisponibles y ListarEventos
(Interactor, IInputPort, IOutputPort, InputDataRequest, OutputDataResponse,
Presenter, ViewModel y Controller del anillo 3, más el controller MVC del
anillo 4). Usá ISolicitudRepository e IEventoRepository del issue #8.

Tené en cuenta que el contrato del mensaje del broker (#44) todavía no está
definido, que no tenemos el connection string del broker y que el login
(#6) todavía no existe. Proponé cómo resolver cada una de esas tres cosas
sin bloquearnos. No crees archivos todavía.

## 2026-10-31

Creá la rama feat/solicitud-reserva desde dev actualizado.

En Availability.UseCases/Solicitudes/SolicitarReserva creá el caso de uso
con el patrón de Tomás: InputDataRequest (EventoId, AsientoId, UsuarioId),
OutputDataResponse, IInputPort, IOutputPort e Interactor. Creá también el
puerto de salida IPublicadorSolicitudes.

El Interactor debe: verificar que el asiento exista en el evento, crear la
SolicitudReserva con un Guid nuevo y la fecha en UTC, guardarla con
ISolicitudRepository, publicarla y entregar el resultado al presenter sin
esperar a Booking. Si el asiento no existe o la publicación falla, debe
informarlo al presenter sin lanzar una excepción.

Justificá en comentarios por qué se guarda antes de publicar, por qué no se
verifica si el asiento está libre y por qué la respuesta no espera a Booking.
Compilá y mostrame el código. No hagas commit.

## 2026-10-31

En Availability.Gateways/Broker creá la clase del mensaje que se publica
(solicitudId, eventoId, asientoId, usuarioId y fechaSolicitud), como
propuesta para el contrato #44, y un PublicadorSolicitudesSimulado que
implemente IPublicadorSolicitudes escribiendo el mensaje en el log.

Igual que ConsultaOcupacionSimulada, que tenga una opción en
appsettings.Development.json para simular que el broker no responde.
Registralo en AddGateways. Justificá en comentarios por qué el usuarioId
viaja en el mensaje, según el contrato de tokens. Compilá.

## 2026-10-31

En Availability.Presenters creá el presenter y el ViewModel de
SolicitarReserva: para el caso exitoso, el número de solicitud y un texto
que aclare que la reserva está en proceso; para los errores, un mensaje
claro para el usuario. En Availability.Controllers creá el controller del
caso de uso, con el mismo estilo que ListarAsientosController. Compilá.

## 2026-10-31

En Availability.Web:
- Registrá el caso de uso en Program.cs con un alias, como hizo Tomás.
- Creá un ReservasController con una acción POST protegida con [Authorize]
  que reciba el evento y el asiento, tome el UsuarioId del claim "sub" del
  usuario logueado y redirija a una página de solicitud recibida
  (Post/Redirect/Get).
- Agregá el Id del evento y de cada asiento al ViewModel de ListarAsientos.
  En Asientos.cshtml, si el usuario inició sesión, habilitá el botón
  Reservar con un formulario con antiforgery que envíe el asiento elegido;
  si no, mostrá un enlace para iniciar sesión que vuelva al mismo mapa.
Compilá.

## 2026-10-31

Ejecutá el sitio y probá en el navegador: reservar un asiento disponible,
pedir un asiento que no existe y simular que el broker no responde.
Revisá que UseCases no dependa de Gateways ni de ASP.NET. Mostrame un
resumen de los cambios y proponé un mensaje de commit con Conventional
Commits. No hagas commit.
