using Booking.Api.Respuestas;
using Booking.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Context;
using Shared.Observability;

namespace Booking.Api.Controllers;

// Capa de presentación de Booking (N-Layer). El controller es delgado: recibe el pedido HTTP, llama a la capa de
// negocio y devuelve el código de estado, sin reglas ni acceso a la base. Cada capa solo llama a la de abajo
// (Api, Business, Data). ConsultaReservas llega por el constructor (inyección de dependencias) y no con "new".
//
// Es una API REST: la ruta nombra el recurso, la acción la da el verbo (acá dos GET de solo lectura) y la
// respuesta va en JSON en vez de una vista. Son los dos endpoints que Availability consulta, según
// contracts/availability-booking.md. Los ids son Guid sin restricción en la ruta: uno inválido responde 400, y
// el 404 queda para "solicitud todavía no procesada".
//
// Auditoría (contracts/logs.md): cada endpoint registra "[Inicio Operación]" y "[Fin Operación]" con IAuditLogger. El
// TraceId no se arma acá: lo deja el middleware UseTraceId con el X-Trace-Id que manda Availability (el solicitudId
// en el endpoint 2), o con un Guid nuevo si el pedido no lo trae. El Fin se registra también si falla, y el motivo
// va como propiedad aparte, igual que en el consumidor del broker.
// [Authorize] en la clase protege los dos endpoints: sin token válido responde 401 (contracts/tokens.md).
[Authorize]
[ApiController]
[Route("api")]
public class ReservasController(ConsultaReservas consulta, IAuditLogger auditoria) : ControllerBase
{
    // Endpoint 1: responde 200 con los asientos ocupados del evento (la lista puede venir vacía).
    [HttpGet("eventos/{eventoId}/asientos-ocupados")]
    public async Task<AsientosOcupadosRespuesta> AsientosOcupados(Guid eventoId)
    {
        return await Auditar<AsientosOcupadosRespuesta>("consultar_asientos_ocupados", async () =>
        {
            var ocupados = await consulta.ObtenerAsientosOcupadosAsync(eventoId);
            return (new AsientosOcupadosRespuesta(eventoId, ocupados), "ok", null);
        });
    }

    // Endpoint 2: responde 200 con el estado de la solicitud, o 404 si Booking todavía no la procesó. Ese 404
    // existe porque Availability responde "recibida" antes de que Booking tome el mensaje del broker (consistencia
    // eventual): por un rato la solicitud existe en Availability y no en Booking, y Availability la muestra "en proceso".
    [HttpGet("solicitudes/{solicitudId}")]
    public async Task<ActionResult<EstadoSolicitud>> EstadoSolicitud(Guid solicitudId)
    {
        return await Auditar<ActionResult<EstadoSolicitud>>("consultar_solicitud", async () =>
        {
            var estado = await consulta.ObtenerEstadoSolicitudAsync(solicitudId);
            // Que Booking todavía no la tenga no es una falla, es el "en proceso": se audita como rechazada
            // con solicitud_inexistente, igual que en Availability.
            return estado is null
                ? ((ActionResult<EstadoSolicitud>)NotFound(), "rechazada", "solicitud_inexistente")
                : ((ActionResult<EstadoSolicitud>)estado, "ok", null);
        });
    }

    // Envuelve una operación con su inicio y su fin. Existe para que los dos endpoints auditen igual y no repitan el
    // try/catch: la operación devuelve su respuesta junto con el Resultado y el Motivo (opcional) que van al Fin.
    // Si lanza una excepción registra el fin con "error" antes de dejarla subir, para que el par inicio/fin quede
    // completo también cuando falla (contracts/logs.md, sección 3). Los textos del Resultado son los de la sección 4.
    private async Task<T> Auditar<T>(string operacion, Func<Task<(T Respuesta, string Resultado, string? Motivo)>> accion)
    {
        auditoria.Inicio(operacion);
        try
        {
            var (respuesta, resultado, motivo) = await accion();
            Fin(operacion, resultado, motivo);
            return respuesta;
        }
        catch (Exception)
        {
            Fin(operacion, "error", "excepcion");
            throw;
        }
    }

    // IAuditLogger.Fin no recibe el motivo: se agrega como propiedad del log con LogContext (AddObservability activa
    // Enrich.FromLogContext), así aparece en Seq junto a la línea de fin sin cambiar el código común del grupo.
    private void Fin(string operacion, string resultado, string? motivo)
    {
        using (motivo is null ? null : LogContext.PushProperty("Motivo", motivo))
            auditoria.Fin(operacion, resultado);
    }
}
