using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsRecoveryToGradebookEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_recovery",
                table: "GradebookEvaluations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_recovery",
                table: "GradebookEvaluations");
        }
    }
}
