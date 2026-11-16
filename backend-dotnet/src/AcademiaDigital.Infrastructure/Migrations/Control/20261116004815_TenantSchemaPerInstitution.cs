using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations.Control
{
    /// <inheritdoc />
    public partial class TenantSchemaPerInstitution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DatabaseName",
                table: "Tenants",
                newName: "SchemaName");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_DatabaseName",
                table: "Tenants",
                newName: "IX_Tenants_SchemaName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SchemaName",
                table: "Tenants",
                newName: "DatabaseName");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_SchemaName",
                table: "Tenants",
                newName: "IX_Tenants_DatabaseName");
        }
    }
}
