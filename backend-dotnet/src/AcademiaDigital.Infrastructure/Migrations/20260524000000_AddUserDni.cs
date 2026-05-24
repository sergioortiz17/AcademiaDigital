using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserDni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM AdminAuditLogs;
                DELETE FROM ActiveSessions;
                DELETE FROM Communications;
                DELETE FROM ContestApplications;
                DELETE FROM Enrollments;
                DELETE FROM TeachingPositions;
                DELETE FROM TeacherContests;
                DELETE FROM SubjectPrerequisites;
                DELETE FROM Subjects;
                DELETE FROM Students;
                DELETE FROM Teachers;
                DELETE FROM Administratives;
                DELETE FROM CooperativeEntities;
                DELETE FROM Careers;
                DELETE FROM Users;
                """);

            migrationBuilder.AddColumn<string>(
                name: "dni",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "dni",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_dni",
                table: "Users",
                column: "dni",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_dni",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "dni",
                table: "Users");
        }
    }
}
