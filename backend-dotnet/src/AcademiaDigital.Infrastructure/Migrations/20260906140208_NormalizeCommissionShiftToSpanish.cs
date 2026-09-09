using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Migración de datos (sin cambio de esquema): unifica Commission.Shift al español
    /// (Mañana/Tarde/Noche), la misma convención que Enrollment.Shift y las constantes de dominio
    /// EnrollmentCapacityPolicy. Antes las comisiones se guardaban en inglés
    /// (Morning/Afternoon/Evening) y nunca coincidían con el turno de la inscripción, lo que
    /// impedía el matching automático de comisión al inscribirse (Parte 6).
    /// Es idempotente: correrla de nuevo sobre datos ya en español no cambia nada.
    /// </summary>
    public partial class NormalizeCommissionShiftToSpanish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Commissions\" SET \"Shift\" = 'Mañana' WHERE \"Shift\" = 'Morning';");
            migrationBuilder.Sql("UPDATE \"Commissions\" SET \"Shift\" = 'Tarde'  WHERE \"Shift\" = 'Afternoon';");
            migrationBuilder.Sql("UPDATE \"Commissions\" SET \"Shift\" = 'Noche'  WHERE \"Shift\" = 'Evening';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Commissions\" SET \"Shift\" = 'Morning'   WHERE \"Shift\" = 'Mañana';");
            migrationBuilder.Sql("UPDATE \"Commissions\" SET \"Shift\" = 'Afternoon' WHERE \"Shift\" = 'Tarde';");
            migrationBuilder.Sql("UPDATE \"Commissions\" SET \"Shift\" = 'Evening'   WHERE \"Shift\" = 'Noche';");
        }
    }
}
