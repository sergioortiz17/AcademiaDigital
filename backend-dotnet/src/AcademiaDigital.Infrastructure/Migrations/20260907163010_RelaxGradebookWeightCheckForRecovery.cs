using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RelaxGradebookWeightCheckForRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GradebookEvaluations_Weight",
                table: "GradebookEvaluations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GradebookEvaluations_Weight",
                table: "GradebookEvaluations",
                sql: "weight_percentage >= 0 AND weight_percentage <= 100 AND (is_recovery OR weight_percentage > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_GradebookEvaluations_Weight",
                table: "GradebookEvaluations");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GradebookEvaluations_Weight",
                table: "GradebookEvaluations",
                sql: "weight_percentage > 0 AND weight_percentage <= 100");
        }
    }
}
