using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TranslateUserRolesToSpanish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Users SET role = 'Alumno' WHERE role = 'Student';
                UPDATE Users SET role = 'Docente' WHERE role = 'Teacher';
                UPDATE Users SET role = 'Secretaria' WHERE role = 'Secretary';
                UPDATE Users SET role = 'TesoreriaCooperadora' WHERE role = 'TreasuryCooperative';
                UPDATE Users SET role = 'Coordinador' WHERE role = 'Coordinator';
                UPDATE Users SET role = 'Administrador' WHERE role = 'Administrator';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Alumno",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Student");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE Users SET role = 'Student' WHERE role = 'Alumno';
                UPDATE Users SET role = 'Teacher' WHERE role = 'Docente';
                UPDATE Users SET role = 'Secretary' WHERE role = 'Secretaria';
                UPDATE Users SET role = 'TreasuryCooperative' WHERE role = 'TesoreriaCooperadora';
                UPDATE Users SET role = 'Coordinator' WHERE role = 'Coordinador';
                UPDATE Users SET role = 'Administrator' WHERE role = 'Administrador';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Student",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Alumno");
        }
    }
}
