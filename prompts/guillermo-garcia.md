# Prompts de Guillermo García

Historial de prompts usados para implementar el TP, en orden cronológico.

## 2026-10-30 10:15

Estoy en la rama feat/api-estado-solicitud-ocupacion y la tarea es la #18 de Booking, la API REST de estado de solicitud y ocupación. Son los dos endpoints que consulta Availability, y tienen que quedar como dice contracts/availability-booking.md. Leé el contrato y el código que ya tiene Booking (la clase Reserva, el BookingDbContext y la tabla reservas) antes de escribir nada.

Los endpoints son dos. El primero es GET /api/eventos/{eventoId}/asientos-ocupados, que devuelve los asientos ocupados del evento, cada uno con su estado, retenida o confirmada. Si el evento no tiene ninguno, devuelve una lista vacía con 200, no un 404. El segundo es GET /api/solicitudes/{solicitudId}, que devuelve el estado de la solicitud y, si fue rechazada, el motivo. Si Booking todavía no la procesó, responde 404 y Availability la muestra como "en proceso".

Hay dos reglas del contrato que no quiero pasar por alto. Una reserva retenida solo cuenta mientras no llegó su hora de vencimiento, y esa hora se compara con la de la base de datos y no con la del servidor. Si ya venció, la solicitud se informa como vencida aunque nadie la haya marcado todavía. Y un id que no sea un Guid tiene que responder 400.

Quiero respetar la estructura que ya tiene Booking: los controllers en Booking.Api, la lógica en Booking.Business y el acceso a la base en Booking.Data. Tiene que levantarse y funcionar, y debemos seguir el modelo de trabajo que nos enseñó el profe en clase: el controller solo recibe el pedido y llama a la clase de negocio, y esa clase recibe sus dependencias por el constructor. Quiero que el código siga ese modelo.

No hagas commit ni push.

## 2026-10-30 11:02

Ya revisé el código. Ahora quiero probarlo, no solo que compile. Levantá la base con docker compose y la API de Booking, cargá reservas de prueba (una retenida vigente, una retenida vencida, una confirmada y una rechazada con pago_fallido) y llamá a los dos endpoints.

Fijate que la retenida vencida no aparezca como ocupada y que la solicitud salga como vencida. Probá también el 404 con una solicitud que no existe y el 400 con un id que no es Guid, en las dos rutas. Si algo falla, avisame qué pasó.

Cuando termines, borrá los datos de prueba.
