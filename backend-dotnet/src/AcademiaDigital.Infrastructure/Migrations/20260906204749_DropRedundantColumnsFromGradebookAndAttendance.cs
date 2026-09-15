using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropRedundantColumnsFromGradebookAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Courses_course_id",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Divisions_division_id",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Gradebooks_Courses_course_id",
                table: "Gradebooks");

            migrationBuilder.DropForeignKey(
                name: "FK_Gradebooks_Divisions_division_id",
                table: "Gradebooks");

            migrationBuilder.DropIndex(
                name: "IX_Gradebooks_course_id_division_id_academic_year_semester",
                table: "Gradebooks");

            migrationBuilder.DropIndex(
                name: "IX_Gradebooks_course_section_id",
                table: "Gradebooks");

            migrationBuilder.DropIndex(
                name: "IX_Gradebooks_division_id",
                table: "Gradebooks");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_course_id_division_id_academic_year_seme~",
                table: "AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_course_section_id",
                table: "AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_division_id",
                table: "AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "academic_year",
                table: "Gradebooks");

            migrationBuilder.DropColumn(
                name: "course_id",
                table: "Gradebooks");

            migrationBuilder.DropColumn(
                name: "division_id",
                table: "Gradebooks");

            migrationBuilder.DropColumn(
                name: "semester",
                table: "Gradebooks");

            migrationBuilder.DropColumn(
                name: "academic_year",
                table: "AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "course_id",
                table: "AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "division_id",
                table: "AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "semester",
                table: "AttendanceSessions");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_course_section_id",
                table: "Gradebooks",
                column: "course_section_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_course_section_id_session_date_start_tim~",
                table: "AttendanceSessions",
                columns: new[] { "course_section_id", "session_date", "start_time", "scope" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Gradebooks_course_section_id",
                table: "Gradebooks");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceSessions_course_section_id_session_date_start_tim~",
                table: "AttendanceSessions");

            migrationBuilder.AddColumn<int>(
                name: "academic_year",
                table: "Gradebooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "course_id",
                table: "Gradebooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "division_id",
                table: "Gradebooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "semester",
                table: "Gradebooks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "academic_year",
                table: "AttendanceSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "course_id",
                table: "AttendanceSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "division_id",
                table: "AttendanceSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "semester",
                table: "AttendanceSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_course_id_division_id_academic_year_semester",
                table: "Gradebooks",
                columns: new[] { "course_id", "division_id", "academic_year", "semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_course_section_id",
                table: "Gradebooks",
                column: "course_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_division_id",
                table: "Gradebooks",
                column: "division_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_course_id_division_id_academic_year_seme~",
                table: "AttendanceSessions",
                columns: new[] { "course_id", "division_id", "academic_year", "semester", "session_date", "start_time", "scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_course_section_id",
                table: "AttendanceSessions",
                column: "course_section_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_division_id",
                table: "AttendanceSessions",
                column: "division_id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Courses_course_id",
                table: "AttendanceSessions",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Divisions_division_id",
                table: "AttendanceSessions",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Gradebooks_Courses_course_id",
                table: "Gradebooks",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Gradebooks_Divisions_division_id",
                table: "Gradebooks",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
