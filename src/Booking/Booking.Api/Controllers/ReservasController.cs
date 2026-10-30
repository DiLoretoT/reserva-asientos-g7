using Booking.Api.Respuestas;
using Booking.Business;
using Microsoft.AspNetCore.Mvc;

namespace Booking.Api.Controllers;

// Los ids van como Guid sin restricción en la ruta: un valor inválido responde 400 (contrato),
// y el 404 queda reservado a "solicitud todavía no procesada".
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

    // Endpoint 2: responde 200 con el estado de la solicitud, o 404 si Booking todavía no la procesó.
    [HttpGet("solicitudes/{solicitudId}")]
    public async Task<ActionResult<EstadoSolicitud>> EstadoSolicitud(Guid solicitudId)
    {
        var estado = await consulta.ObtenerEstadoSolicitudAsync(solicitudId);
        return estado is null ? NotFound() : estado;
    }
}
