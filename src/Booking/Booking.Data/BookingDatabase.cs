using Dapper;
using Npgsql;

namespace Booking.Data;

public sealed class BookingDatabase(NpgsqlDataSource dataSource)
{
    public ValueTask<NpgsqlConnection> AbrirConexionAsync(CancellationToken cancellationToken = default)
        => dataSource.OpenConnectionAsync(cancellationToken);

    public async Task<DateTime> ObtenerHoraUtcAsync(CancellationToken cancellationToken = default)
    {
        await using var conexion = await AbrirConexionAsync(cancellationToken);
        return await conexion.QuerySingleAsync<DateTime>(
            new CommandDefinition("SELECT CURRENT_TIMESTAMP", cancellationToken: cancellationToken));
    }
}
