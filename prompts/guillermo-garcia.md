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

## 2026-10-31 13:05

Estoy en la rama feat/retener-o-rechazar-asiento y la tarea es la #13 de Booking, retener o rechazar el asiento, que es el paso 4 del flujo. Cuando una réplica de Booking toma una solicitud de reserva, tiene que fijarse si el asiento está libre. Si está libre crea una reserva retenida con su vencimiento, y si está ocupado la rechaza con motivo asiento_ocupado.

Antes de escribir nada revisá cómo está armado Booking. Leé el contrato contracts/availability-booking.md para ver qué se considera un asiento ocupado, la clase Reserva, el BookingDbContext y la clase ConsultaReservas. Fijate sobre todo que el BookingDbContext ya tiene un índice único por evento y asiento, que solo cuenta las reservas retenidas o confirmadas. Quiero reutilizar eso y no inventar nada nuevo.

Booking es N-Layer, así que la lógica va en Booking.Business como una clase que recibe el DbContext y las opciones por el constructor, igual que ConsultaReservas, y se registra en Program.cs. El tiempo de retención ya está en la configuración como RetentionMinutes.

El vencimiento de las reservas retenidas es otro requermiento en github , lo mismo la lectura del broker. No toques nada de eso, y si algo queda pendiente por eso, dejalo anotado en un comentario.

Tene en cuenta la condición de carrera: varias réplicas pueden pedir el mismo asiento al mismo tiempo. No quiero consultar primero si está libre y después insertar, porque otra réplica se podría colar en el medio y las dos lo retendrían. Quiero insertar la reserva retenida y dejar que decida el índice: si la base responde con error, guardá la solicitud como rechazada con asiento_ocupado.

Dos cuidados. Primero, cualquier otro error de la base tiene que salir como error y no tomarse como asiento ocupado, así que fijate en el nombre del índice y no solo en el código de error. Segundo, si la misma solicitud llega dos veces, que devuelva la reserva que ya existe y no cree otra, porque el broker puede repetir mensajes.

No hagas commit ni push.

## 2026-10-31 13:40

Ahora quiero probar lo que hicimos contra la base de verdad, no solo que compile. Levantá la base con docker compose y probá cuatro casos.

El primero es un asiento libre, que tiene que quedar retenida con su fecha de vencimiento. El segundo es un asiento que ya está ocupado, que tiene que quedar rechazada con motivo asiento_ocupado. El tercero es la misma solicitud enviada dos veces, que no tiene que crear una segunda reserva sino devolver la primera. El cuarto proba 20 solicitudes distintas a la vez por el mismo asiento, donde tiene que quedar una sola retenida y las otras 19 rechazadas.

Hacé la prueba que sea de manera temporal fuera del repositorio, para no agregar un proyecto de tests ni paquetes nuevos. Si algo falla, avisame qué pasó y no lo arregles a ciegas. Cuando termines borrá los datos de prueba y detené el contenedor de la base.

Además verificá que el proyecto compile sin errores ni advertencias y que no se haya cambiado nada del código que ya funcionaba.

No hagas commit ni push.
