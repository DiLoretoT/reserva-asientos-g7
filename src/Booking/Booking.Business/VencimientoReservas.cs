using Booking.Data;
using Booking.Domain;
using Microsoft.EntityFrameworkCore;

namespace Booking.Business;

public class VencimientoReservas(BookingDbContext db)
{
    public Task<int> VencerAsync(CancellationToken cancellationToken = default)
        => db.Reservas
            .Where(r => r.Estado == EstadoReserva.Retenida
                && r.FechaVencimiento <= DateTime.UtcNow)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Estado, EstadoReserva.Vencida)
                .SetProperty(r => r.FechaActualizacion, r => DateTime.UtcNow),
                cancellationToken);
}
