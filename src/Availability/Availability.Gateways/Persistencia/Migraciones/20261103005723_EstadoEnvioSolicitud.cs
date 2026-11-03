using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Availability.Gateways.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class EstadoEnvioSolicitud : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las solicitudes que ya estaban en la base quedan como Enviada: antes del patrón
            // Outbox, una solicitud solo quedaba guardada si el broker la había recibido, porque
            // si la publicación fallaba se borraba.
            migrationBuilder.AddColumn<string>(
                name: "EstadoEnvio",
                table: "Solicitudes",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Enviada");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_EstadoEnvio",
                table: "Solicitudes",
                column: "EstadoEnvio");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_EstadoEnvio",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "EstadoEnvio",
                table: "Solicitudes");
        }
    }
}
