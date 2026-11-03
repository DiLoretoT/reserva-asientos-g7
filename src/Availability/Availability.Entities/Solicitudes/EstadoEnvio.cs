namespace Availability.Entities.Solicitudes
{
    // Si la solicitud ya salió hacia el broker. Es un dato de Availability y no el estado de
    // la reserva (retenida, confirmada, rechazada, vencida), que sigue siendo de Booking.
    public enum EstadoEnvio
    {
        // Quedó guardada pero todavía no se pudo publicar: el broker no respondió. La reenvía
        // el caso de uso ReintentarEnvio.
        Pendiente,

        // El broker confirmó la publicación.
        Enviada
    }
}
