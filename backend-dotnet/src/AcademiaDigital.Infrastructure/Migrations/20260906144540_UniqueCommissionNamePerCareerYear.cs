using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <summary>
    /// Constraint única (CareerId, AcademicYear, Name) en Commissions — mismo patrón que ya existía
    /// para Code. Evita que un profesor vea dos comisiones con el mismo nombre y cargue notas en la
    /// equivocada.
    ///
    /// Antes de crear el índice se DES-DUPLICAN los nombres existentes que ya lo violarían (todas
    /// comisiones ad-hoc de dev-tools/testing, con Code único pero Name repetido): se les agrega su
    /// Code —único dentro de carrera+año— al final del Name. Es no destructivo (renombra, no borra
    /// filas referenciadas por cargos/planillas) e idempotente (solo toca las que están en un grupo
    /// duplicado; si se corre de nuevo, ya no hay grupos duplicados que tocar).
    /// </summary>
    public partial class UniqueCommissionNamePerCareerYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Commissions"" c
                SET ""Name"" = c.""Name"" || ' [' || c.""Code"" || ']'
                WHERE EXISTS (
                    SELECT 1 FROM ""Commissions"" d
                    WHERE d.""CareerId"" = c.""CareerId""
                      AND d.""AcademicYear"" = c.""AcademicYear""
                      AND d.""Name"" = c.""Name""
                      AND d.""Id"" <> c.""Id""
                );");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_CareerId_AcademicYear_Name",
                table: "Commissions",
                columns: new[] { "CareerId", "AcademicYear", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Commissions_CareerId_AcademicYear_Name",
                table: "Commissions");
            // El renombrado de datos no se revierte (los nombres con sufijo [Code] siguen siendo
            // válidos y únicos); revertirlo reintroduciría los duplicados.
        }
    }
}
