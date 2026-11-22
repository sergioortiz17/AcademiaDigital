using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradebookDatesAbsencesAndHealthInsurance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "health_insurance",
                table: "Teachers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "health_insurance",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_absent",
                table: "GradeEntryRevisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "evaluation_date",
                table: "GradebookEvaluations",
                type: "date",
                nullable: true);

            // Requisito de ingreso por defecto, solo si la institución todavía no cargó ninguno:
            // sin requisitos, la sección "Requisitos de ingreso" del alumno quedaría vacía.
            migrationBuilder.Sql("""
                INSERT INTO "DocumentRequirements" ("Code", "Name", "Description", "IsRequired", "IsActive")
                SELECT 'ANALITICO', 'Analítico de nivel secundario',
                       'Copia del analítico (o constancia de título en trámite) en PDF, JPG o PNG.', TRUE, TRUE
                WHERE NOT EXISTS (SELECT 1 FROM "DocumentRequirements");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "health_insurance",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "health_insurance",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "is_absent",
                table: "GradeEntryRevisions");

            migrationBuilder.DropColumn(
                name: "evaluation_date",
                table: "GradebookEvaluations");
        }
    }
}
