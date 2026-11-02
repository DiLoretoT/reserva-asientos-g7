# Prompts de Facundo

Historial de prompts usados para implementar el TP, en orden cronológico.

## 2026-10-29

- parandonos en dev crea mi rama docs/contrato-mensaje-broker pero no la subas todavia.
- si empecemos, ve haciendome preguntas para que definamos todo el contrato, ademas recuerda de documentar mis propmts en prompts/prompts-facundo.md.

## 2026-11-02

Sesión de revisión del PR #85 (contrato del mensaje del broker de Jonathan) y del issue #44.

Decisiones que tomé:

- Completar el mensaje al guardar la reserva retenida, y pasar a completarlo tras el resultado final solo si Axel confirma idempotencia de Payment por BookingId.
- No borrar la solicitud cuando el envío al broker es incierto: marcarla como no publicada y reintentar con el mismo MessageId (le toca a Availability, la compensación actual del Interactor borra).
- El contrato queda en cinco campos, sin monto: Booking usa un importe por configuración al llamar a Payment.
- La configuración del broker va en un PR aparte (docs/broker-configuracion, PR #86) y el publicador real, detrás de IPublicadorSolicitudes, lo implemento yo. En validación, mantener ContentType y Subject, sacar version y dejar la lista corta de motivos de DLQ.

Correcciones que le pedí a mi agente sobre el borrador del contrato:

- Quitar el outbox completo y el 202 Accepted que había propuesto: el código compensa borrando la solicitud, ese es el comportamiento real.
- Reagregar fechaSolicitud al envelope: el mensaje del código ya la manda.
- Cerrar pendientes que el código ya respondía: retención Booking:RetentionMinutes = 5, índice parcial único ux_reserva_asiento_activo, idempotencia existente, usuarioId claim sub de tokens.md, en lugar de "ademas usa".
- Llamar a los parámetros reales de Payment: BookingId, no reservaId.
- No decir "Propuesta confirmada" ni nada que no esté decidido: "Propuesta" solo.
- Declarar que el índice puede bloquear hasta 15 s un asiento que el REST ya considera libre, no afirmar que siempre coincide.
- Renombrar el archivo al formato del README.

Resultado: review del PR #85 publicada, PR #86 (configuración del broker) en borrador y comentario en el issue #44.
