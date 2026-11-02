using Booking.Api.Respuestas;
using Booking.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

// Capa de presentación de Booking (N-Layer). El controller es delgado: recibe el pedido HTTP, llama a la capa de
// negocio y devuelve el código de estado, sin reglas ni acceso a la base. Cada capa solo llama a la de abajo
// (Api, Business, Data). ConsultaReservas llega por el constructor (inyección de dependencias) y no con "new".
//
// Es una API REST: la ruta nombra el recurso, la acción la da el verbo (acá dos GET de solo lectura) y la
// respuesta va en JSON en vez de una vista. Son los dos endpoints que Availability consulta, según
// contracts/availability-booking.md. Los ids son Guid sin restricción en la ruta: uno inválido responde 400, y
// el 404 queda para "solicitud todavía no procesada".
// [Authorize] en la clase protege los dos endpoints: sin token válido responde 401 (contracts/tokens.md).
[Authorize]
[ApiController]
[Route("api")]
public class ReservasController(ConsultaReservas consulta) : ControllerBase
{
    // Endpoint 1: responde 200 con los asientos ocupados del evento (la lista puede venir vacía).
    [HttpGet("eventos/{eventoId}/asientos-ocupados")]
    public async Task<AsientosOcupadosRespuesta> AsientosOcupados(Guid eventoId)
    {
        var ocupados = await consulta.ObtenerAsientosOcupadosAsync(eventoId);
        return new AsientosOcupadosRespuesta(eventoId, ocupados);
    }

    // Endpoint 2: responde 200 con el estado de la solicitud, o 404 si Booking todavía no la procesó. Ese 404
    // existe porque Availability responde "recibida" antes de que Booking tome el mensaje del broker (consistencia
    // eventual): por un rato la solicitud existe en Availability y no en Booking, y Availability la muestra "en proceso".
    [HttpGet("solicitudes/{solicitudId}")]
    public async Task<ActionResult<EstadoSolicitud>> EstadoSolicitud(Guid solicitudId)
    {
        var estado = await consulta.ObtenerEstadoSolicitudAsync(solicitudId);
        return estado is null ? NotFound() : estado;
    }
}
