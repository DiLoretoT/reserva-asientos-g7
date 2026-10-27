# Prompts de Tomás Di Loreto

Historial de prompts usados para implementar el TP, en orden cronológico.

## 2026-10-25 19:12

Background (from the session that started this one):
Sistema de reserva de asientos con microservicios en .NET 10: Availability (sitio MVC, Clean Architecture, dominio anémico), Booking (API REST, N-Layer), Payment (API gRPC, N-Layer), servidor de autorización y Azure Service Bus como broker. Grupo de 9 personas; Tomás coordina y hace Availability con Matías. Estado: repo recién clonado y vacío, sin commits en `main`. Plan: subir la estructura de proyectos a `main` (único push directo), crear `dev` como rama por defecto, proteger `dev` y `main` con PR y una aprobación. Reglas: en el repo NO van CLAUDE.md, AGENTS.md ni instrucciones para IA; tampoco minutas ni documentos de coordinación. Ramas `<tipo>/<slug>`. Commits con marcas AI-full/AI-assisted permitidas en este repo (`git config guard.permitirMarcasIA true` ya está). Nunca commit ni push sin OK explícito de Tomás en el turno. Un hook local (`.claude/`, excluido de git) registra cada prompt de Tomás en `prompts/tomas-di-loreto.md`. La estructura de referencia está en `F:\VScode\UTN\practica-prog4` (solo la estructura de proyectos; el código de práctica de Availability, su CLAUDE.md y `docs/componentes-y-tareas.md` no van al repo). Tomás aprende .NET y C# desde cero: explicá en español, simple, sin asumir conocimientos.

Task:
[brief de sesión] Esta es la sesión de implementación del TP grupal de Programación IV (UTN), en el repo `F:\VScode\UTN\reserva-asientos-g7` (remoto `https://github.com/DiLoretoT/reserva-asientos-g7`). Tomás te va a ir mandando prompts paso a paso. No hagas nada hasta recibir su primer prompt: respondé solo "Listo, esperando el primer paso."

Shared notes folder for this group of sessions (outside the repo; read siblings' notes there, leave hand-over notes or large findings there instead of pasting them into messages): C:\Users\tomas_7qljag7\AppData\Local\Claude-Data\claude_pzs8sxrjxfjjc\side-session-notes\local_8f3632ba-aa52-4a67-b8f5-0548befacd0e

## 2026-10-26 21:09

Empecemos a trabajar en Availability, el sitio público del TP de reserva de asientos (Clean Architecture, dominio anémico). Revisá el issue #2 del repo de gh, que es por donde comenzamos.

Tomá como referencia el ejemplo de Clean Architecture del profesor: F:\VScode\UTN\Tecnicatura Universitaria en Programacion\4to Cuatrimestre\Programacion IV\Unidad I\CleanArchitectureUncleBob.zip (fijate cómo arma Agenda.Entities y en qué se diferencia de las entidades de persistencia de Agenda.Gateways).

Para empezar a trabajar creá la rama feat/entidades-evento-asiento desde dev y armá en Availability.Entities las entidades Evento y Asiento, anémicas, siguiendo el estilo del ejemplo. Justificá en comentarios las decisiones de diseño, por ejemplo qué datos van en el dominio y cuáles quedan para la persistencia.

Compilá la solución y mostrame el código. Todavía no hagas commit.

## 2026-10-26 21:48

agregale a Asiento un Id Guid, por la misma razón que Evento, y actualizá el comentario

## 2026-10-26 21:55

No, yo hago el add, commit y push
