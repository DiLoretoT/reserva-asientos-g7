using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitud_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asiento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<string>(type: "text", nullable: false),
                    estado = table.Column<string>(type: "text", nullable: false),
                    motivo_rechazo = table.Column<string>(type: "text", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    fecha_vencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("reservas_pkey", x => x.id);
                    table.UniqueConstraint("reservas_solicitud_id_key", x => x.solicitud_id);
                    table.CheckConstraint("ck_reservas_estado", "estado IN ('retenida', 'confirmada', 'rechazada', 'vencida')");
                    table.CheckConstraint("ck_reservas_estado_motivo", "(estado = 'rechazada' AND motivo_rechazo IS NOT NULL) OR (estado <> 'rechazada' AND motivo_rechazo IS NULL)");
                    table.CheckConstraint("ck_reservas_motivo", "motivo_rechazo IN ('asiento_ocupado', 'pago_fallido', 'pago_no_disponible')");
                    table.CheckConstraint("ck_reservas_usuario", "usuario_id ~ '[^[:space:]]'");
                    table.CheckConstraint("ck_reservas_usuario_largo", "length(usuario_id) <= 200");
                    table.CheckConstraint("ck_reservas_vencimiento", "estado = 'rechazada' OR fecha_vencimiento IS NOT NULL");
                });

            migrationBuilder.CreateIndex(
                name: "ix_reservas_retenciones_vencimiento",
                table: "reservas",
                column: "fecha_vencimiento",
                filter: "estado = 'retenida'");

            migrationBuilder.CreateIndex(
                name: "ux_reserva_asiento_activo",
                table: "reservas",
                columns: new[] { "evento_id", "asiento_id" },
                unique: true,
                filter: "estado IN ('retenida', 'confirmada')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservas");
        }
    }
}
