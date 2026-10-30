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