using Availability.Entities.Eventos;
using Availability.Entities.Solicitudes;
using Availability.Gateways.Persistencia.Entidades;

namespace Availability.Gateways.Persistencia
{
    // Conversión entre entidades de persistencia (las de la base) y entidades de dominio
    // (las de Availability.Entities). Es la frontera entre los dos anillos: los repositorios
    // reciben y devuelven entidades de dominio, y solo acá adentro se ve la forma de la base.
    //
    // Se mapea a mano, sin librerías como AutoMapper, porque son pocas propiedades y así
    // queda explícito qué dato pasa de un lado al otro (por ejemplo, que EventoId no llega
    // al dominio).
    internal static class Mapeos
    {
        public static Evento ADominio(this EventoDb evento) =>
            new()
            {
                Id = evento.Id,
                Nombre = evento.Nombre,
                Fecha = evento.Fecha,
                Asientos = evento.Asientos.Select(a => a.ADominio()).ToList()
            };

        public static Asiento ADominio(this AsientoDb asiento) =>
            new()
            {
                Id = asiento.Id,
                Fila = asiento.Fila,
                Numero = asiento.Numero
            };

        public static SolicitudReserva ADominio(this SolicitudReservaDb solicitud) =>
            new()
            {
                Id = solicitud.Id,
                EventoId = solicitud.EventoId,
                AsientoId = solicitud.AsientoId,
                UsuarioId = solicitud.UsuarioId,
                FechaSolicitud = solicitud.FechaSolicitud
            };

        public static SolicitudReservaDb APersistencia(this SolicitudReserva solicitud) =>
            new()
            {
                Id = solicitud.Id,
                EventoId = solicitud.EventoId,
                AsientoId = solicitud.AsientoId,
                UsuarioId = solicitud.UsuarioId,
                FechaSolicitud = solicitud.FechaSolicitud
            };
    }
}
