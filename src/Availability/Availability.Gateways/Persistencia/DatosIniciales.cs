using Availability.Gateways.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Availability.Gateways.Persistencia
{
    // Datos iniciales (seed) de la base: eventos de prueba con sus asientos. Entity
    // Framework los incluye en la migración, así que cualquier integrante que cree la base
    // tiene exactamente los mismos datos, y la demo siempre arranca igual.
    //
    // Los Guid son fijos y no Guid.NewGuid(): si cambiaran en cada ejecución, Entity
    // Framework generaría una migración nueva cada vez, y además el resto del grupo
    // (Booking, las pruebas de integración) no podría usar IDs conocidos.
    //
    // Formato de los Guid, elegido para que se lean a simple vista:
    //   evento:  0000000E-0000-0000-0000-000000000000          (E = número de evento)
    //   asiento: 0000000E-00FF-NNNN-0000-000000000000          (FF = fila, NNNN = número)
    // Por ejemplo, 00000001-0002-0012-0000-000000000000 es el asiento B-12 del evento 1.
    public static class DatosIniciales
    {
        private const string Filas = "ABCDEFGHIJ";

        public static void Cargar(ModelBuilder modelBuilder)
        {
            var eventos = new List<EventoDb>
            {
                CrearEvento(1, "Recital de prueba", new DateTime(2026, 12, 12, 23, 0, 0, DateTimeKind.Utc)),
                CrearEvento(2, "Conferencia de .NET", new DateTime(2026, 11, 20, 21, 0, 0, DateTimeKind.Utc)),
                CrearEvento(3, "Función de teatro", new DateTime(2026, 12, 5, 22, 30, 0, DateTimeKind.Utc)),
            };

            var asientos = new List<AsientoDb>();
            asientos.AddRange(CrearAsientos(1, cantidadFilas: 5, asientosPorFila: 10));
            asientos.AddRange(CrearAsientos(2, cantidadFilas: 3, asientosPorFila: 8));
            asientos.AddRange(CrearAsientos(3, cantidadFilas: 4, asientosPorFila: 6));

            modelBuilder.Entity<EventoDb>().HasData(eventos);
            modelBuilder.Entity<AsientoDb>().HasData(asientos);
        }

        public static Guid IdEvento(int numeroEvento) =>
            Guid.Parse($"{numeroEvento:D8}-0000-0000-0000-000000000000");

        public static Guid IdAsiento(int numeroEvento, int numeroFila, int numero) =>
            Guid.Parse($"{numeroEvento:D8}-{numeroFila:D4}-{numero:D4}-0000-000000000000");

        private static EventoDb CrearEvento(int numeroEvento, string nombre, DateTime fecha) =>
            new()
            {
                Id = IdEvento(numeroEvento),
                Nombre = nombre,
                Fecha = fecha
            };

        private static IEnumerable<AsientoDb> CrearAsientos(int numeroEvento, int cantidadFilas, int asientosPorFila)
        {
            for (var fila = 1; fila <= cantidadFilas; fila++)
            {
                for (var numero = 1; numero <= asientosPorFila; numero++)
                {
                    yield return new AsientoDb
                    {
                        Id = IdAsiento(numeroEvento, fila, numero),
                        Fila = Filas[fila - 1].ToString(),
                        Numero = numero,
                        EventoId = IdEvento(numeroEvento)
                    };
                }
            }
        }
    }
}
