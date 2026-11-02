# Prompts de Facundo

Historial de prompts usados para implementar el TP, en orden cronológico.

## 2026-10-29

- parandonos en dev crea mi rama docs/contrato-mensaje-broker pero no la subas todavia.
- si empecemos, ve haciendome preguntas para que definamos todo el contrato, ademas recuerda de documentar mis propmts en prompts/prompts-facundo.md.

## Reconstruido: del 27/10 al 2/11

Reconstruido de memoria, resumido, no literal. Fechas puestas solo donde me consta; lo demas va entre el 29/10 y el 2/11.

- Arranco el contrato del mensaje de cero con el agente, y la primera version larga va con cola unica, outbox completo con 202, doble idempotencia y traceId aparte. Despues cambio: por la topologia de topico y suscripcion (necesitamos copias por integrante en desarrollo), porque el outbox con 202 lo dejo fuera y el codigo compensa borrando la solicitud, porque la idempotencia queda solo en la clave unica sobre solicitudId, y porque la traza es el solicitudId sin propiedad aparte (esa la cerro esa misma noche del 28/10 con la propuesta de envelope).
- El 29/10 hacemos la ronda de preguntas y cierro la redaccion: topico grupo7-booking-topic, suscripcion booking (booking-<nombre> por integrante en desarrollo), LockDuration, reintentos y DLQ.
- Entre el 31/10 y el 2/11 le pido al agente contrastar el contrato con el repo, y de ahi cambio varias cosas: traza igual a solicitudId, fechaSolicitud de vuelta (el mensaje del codigo ya la manda), usuarioId texto con el claim sub, retencion cerrada con Booking:RetentionMinutes = 5, el parametro real de Payment es BookingId, y solo la clave unica de solicitudId en idempotencia. Despues cambio: con lo acordado en el PR #85 el monto sale del mensaje (Booking usa un importe por configuracion) y la fecha ya no se rechaza por no terminar en Z.
- El 2/11 reviso el PR #85 de Jonathan y decido no subir mi propio contrato: completo al guardar la retenida, no borro la solicitud si el envio es incierto, cinco campos, lista corta de motivos de DLQ y la configuracion del broker en un PR aparte (#86). El publicador real lo implemento yo.
- Use un asistente de consulta para discutir las decisiones arquitectonicas de los PRs antes de ejecutarlas en OpenCode.

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

Resultado: review del PR #85 publicada, PR #86 (configuración del broker) en borrador, comentario en el issue #44, y este archivo renombrado al formato del README.
