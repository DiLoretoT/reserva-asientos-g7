# Prompts de Tomás Di Loreto

Historial de prompts usados para implementar el TP, en orden cronológico.

## 2026-10-26 21:09

Empecemos a trabajar en Availability, el sitio público del TP de reserva de asientos (Clean Architecture, dominio anémico). Revisá el issue #2 del repo de gh, que es por donde comenzamos.

Tomá como referencia el ejemplo de Clean Architecture del profesor: F:\VScode\UTN\Tecnicatura Universitaria en Programacion\4to Cuatrimestre\Programacion IV\Unidad I\CleanArchitectureUncleBob.zip (fijate cómo arma Agenda.Entities y en qué se diferencia de las entidades de persistencia de Agenda.Gateways).

Para empezar a trabajar creá la rama feat/entidades-evento-asiento desde dev y armá en Availability.Entities las entidades Evento y Asiento, anémicas, siguiendo el estilo del ejemplo. Justificá en comentarios las decisiones de diseño, por ejemplo qué datos van en el dominio y cuáles quedan para la persistencia.

Compilá la solución y mostrame el código. Todavía no hagas commit.

## 2026-10-26 21:48

agregale a Asiento un Id Guid, por la misma razón que Evento, y actualizá el comentario

## 2026-10-26 21:55

No, yo hago el add, commit y push

## 2026-10-26 23:39

Estoy en la rama docs/contrato-availability-booking. Vamos ahora con el issue #43 (contrato Availability y Booking por REST).

Availability necesita dos datos que tiene Booking: 1 qué asientos de un evento están ocupados (paso 1 del enunciado) y en qué estado quedó una solicitud de reserva (paso 7). Para que estemos alineados con la pareja que se encargad e booking, quiero dejar escrito el contrato REST como propuesta, en un PR que Booking pueda comentar.
Criterios: un asiento está ocupado si tiene una reserva retenida o confirmada. Booking no sabe qué asientos existen, eso lo sabe Availability, así que Booking solo informa los ocupados. Entre que Availability publica la solicitud y Booking la procesa, la solicitud no existe en Booking y ese es el tramo de consistencia eventual.

Pedido:
Escribí contracts/availability-booking.md como para que lo puedan leer y aprobar los chicos de la otra pareja, con dos endpoints de Booking:
1. Asientos ocupados de un evento: GET con el eventoId, responde los Id de los asientos ocupados.
2. Estado de una solicitud: GET con el solicitudId, responde el estado (retenida, confirmada, rechazada o vencida) y el motivo si fue rechazada (asiento ocupado, pago fallido o pago no disponible). Si Booking todavía no la procesó, responde 404 y Availability la muestra como "en proceso".
Para cada uno: ruta, parámetros, ejemplo de respuesta en JSON y códigos de estado. Los Id son Guid. Las llamadas llevan el token de servicio en el header Authorization; el detalle queda pendiente del contrato de tokens (#45).
Reviso cuando termines, te digo si hay que cambiar algo y yo me encargo de commit, push y demás

