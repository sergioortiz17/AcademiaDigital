using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAnnualToStudyPlanCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_annual",
                table: "StudyPlanCourses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Mark annual subjects in DS2023 plan
            migrationBuilder.Sql(@"
UPDATE spc
SET spc.[is_annual] = 1
FROM [StudyPlanCourses] spc
INNER JOIN [Courses] c ON c.[id] = spc.[course_id]
WHERE c.[name] IN (
    'Elementos de matemática y lógica',
    'Sistemas y organizaciones',
    'Programación I',
    'Base de datos',
    'Inglés',
    'Estadística y probabilidad aplicadas',
    'Modelado y Arquitectura de Software',
    'Programación II',
    'Práctica Profesionalizante I',
    'Interfaz de usuario',
    'Ingeniería de software',
    'Programación III',
    'Práctica Profesionalizante II'
);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_annual",
                table: "StudyPlanCourses");
        }
    }
}
