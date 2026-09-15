using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTeachingPositionToCourseSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_TeachingPositions_teaching_position_id",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_TeachingPositions_teaching_position_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Gradebooks_TeachingPositions_teaching_position_id",
                table: "Gradebooks");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherAssignments_TeachingPositions_teaching_position_id",
                table: "TeacherAssignments");

            // Renombrar la tabla (preserva datos + FKs a Courses/Divisions/Teachers/Users) en vez de
            // drop+create. Se renombran PK, índices, FKs y el check-constraint a los nombres nuevos.
            migrationBuilder.RenameTable(name: "TeachingPositions", newName: "CourseSections");
            migrationBuilder.Sql("ALTER TABLE \"CourseSections\" RENAME CONSTRAINT \"PK_TeachingPositions\" TO \"PK_CourseSections\";");
            migrationBuilder.Sql("ALTER TABLE \"CourseSections\" RENAME CONSTRAINT \"CK_TeachingPositions_AssignmentState\" TO \"CK_CourseSections_AssignmentState\";");
            migrationBuilder.Sql("ALTER TABLE \"CourseSections\" RENAME CONSTRAINT \"FK_TeachingPositions_Courses_course_id\" TO \"FK_CourseSections_Courses_course_id\";");
            migrationBuilder.Sql("ALTER TABLE \"CourseSections\" RENAME CONSTRAINT \"FK_TeachingPositions_Teachers_teacher_id\" TO \"FK_CourseSections_Teachers_teacher_id\";");
            migrationBuilder.Sql("ALTER TABLE \"CourseSections\" RENAME CONSTRAINT \"FK_TeachingPositions_Users_deactivated_by_user_id\" TO \"FK_CourseSections_Users_deactivated_by_user_id\";");
            migrationBuilder.Sql("ALTER TABLE \"CourseSections\" RENAME CONSTRAINT \"FK_TeachingPositions_Divisions_division_id\" TO \"FK_CourseSections_Divisions_division_id\";");
            migrationBuilder.RenameIndex(name: "IX_TeachingPositions_academic_year_semester_is_active", table: "CourseSections", newName: "IX_CourseSections_academic_year_semester_is_active");
            migrationBuilder.RenameIndex(name: "IX_TeachingPositions_division_id_course_id", table: "CourseSections", newName: "IX_CourseSections_division_id_course_id");
            migrationBuilder.RenameIndex(name: "IX_TeachingPositions_course_id", table: "CourseSections", newName: "IX_CourseSections_course_id");
            migrationBuilder.RenameIndex(name: "IX_TeachingPositions_deactivated_by_user_id", table: "CourseSections", newName: "IX_CourseSections_deactivated_by_user_id");
            migrationBuilder.RenameIndex(name: "IX_TeachingPositions_teacher_id", table: "CourseSections", newName: "IX_CourseSections_teacher_id");
            migrationBuilder.AddColumn<bool>(name: "is_annual", table: "CourseSections", type: "boolean", nullable: false, defaultValue: false);

            migrationBuilder.RenameColumn(
                name: "teaching_position_id",
                table: "TeacherAssignments",
                newName: "course_section_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherAssignments_teaching_position_id",
                table: "TeacherAssignments",
                newName: "IX_TeacherAssignments_course_section_id");

            migrationBuilder.RenameColumn(
                name: "teaching_position_id",
                table: "Gradebooks",
                newName: "course_section_id");

            migrationBuilder.RenameIndex(
                name: "IX_Gradebooks_teaching_position_id",
                table: "Gradebooks",
                newName: "IX_Gradebooks_course_section_id");

            migrationBuilder.RenameColumn(
                name: "teaching_position_id",
                table: "Enrollments",
                newName: "course_section_id");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_teaching_position_id",
                table: "Enrollments",
                newName: "IX_Enrollments_course_section_id");

            migrationBuilder.RenameColumn(
                name: "teaching_position_id",
                table: "AttendanceSessions",
                newName: "course_section_id");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_teaching_position_id",
                table: "AttendanceSessions",
                newName: "IX_AttendanceSessions_course_section_id");

            migrationBuilder.CreateTable(
                name: "TeacherCareers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeacherId = table.Column<long>(type: "bigint", nullable: false),
                    CareerId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherCareers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherCareers_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherCareers_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherCareers_CareerId",
                table: "TeacherCareers",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherCareers_TeacherId_CareerId",
                table: "TeacherCareers",
                columns: new[] { "TeacherId", "CareerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_CourseSections_course_section_id",
                table: "AttendanceSessions",
                column: "course_section_id",
                principalTable: "CourseSections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_CourseSections_course_section_id",
                table: "Enrollments",
                column: "course_section_id",
                principalTable: "CourseSections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Gradebooks_CourseSections_course_section_id",
                table: "Gradebooks",
                column: "course_section_id",
                principalTable: "CourseSections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherAssignments_CourseSections_course_section_id",
                table: "TeacherAssignments",
                column: "course_section_id",
                principalTable: "CourseSections",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_CourseSections_course_section_id",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_CourseSections_course_section_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Gradebooks_CourseSections_course_section_id",
                table: "Gradebooks");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherAssignments_CourseSections_course_section_id",
                table: "TeacherAssignments");

            migrationBuilder.DropTable(
                name: "TeacherCareers");

            migrationBuilder.RenameTable(name: "CourseSections", newName: "TeachingPositions");
            migrationBuilder.Sql("ALTER TABLE \"TeachingPositions\" RENAME CONSTRAINT \"PK_CourseSections\" TO \"PK_TeachingPositions\";");
            migrationBuilder.Sql("ALTER TABLE \"TeachingPositions\" RENAME CONSTRAINT \"CK_CourseSections_AssignmentState\" TO \"CK_TeachingPositions_AssignmentState\";");
            migrationBuilder.Sql("ALTER TABLE \"TeachingPositions\" RENAME CONSTRAINT \"FK_CourseSections_Courses_course_id\" TO \"FK_TeachingPositions_Courses_course_id\";");
            migrationBuilder.Sql("ALTER TABLE \"TeachingPositions\" RENAME CONSTRAINT \"FK_CourseSections_Teachers_teacher_id\" TO \"FK_TeachingPositions_Teachers_teacher_id\";");
            migrationBuilder.Sql("ALTER TABLE \"TeachingPositions\" RENAME CONSTRAINT \"FK_CourseSections_Users_deactivated_by_user_id\" TO \"FK_TeachingPositions_Users_deactivated_by_user_id\";");
            migrationBuilder.Sql("ALTER TABLE \"TeachingPositions\" RENAME CONSTRAINT \"FK_CourseSections_Divisions_division_id\" TO \"FK_TeachingPositions_Divisions_division_id\";");
            migrationBuilder.RenameIndex(name: "IX_CourseSections_academic_year_semester_is_active", table: "TeachingPositions", newName: "IX_TeachingPositions_academic_year_semester_is_active");
            migrationBuilder.RenameIndex(name: "IX_CourseSections_division_id_course_id", table: "TeachingPositions", newName: "IX_TeachingPositions_division_id_course_id");
            migrationBuilder.RenameIndex(name: "IX_CourseSections_course_id", table: "TeachingPositions", newName: "IX_TeachingPositions_course_id");
            migrationBuilder.RenameIndex(name: "IX_CourseSections_deactivated_by_user_id", table: "TeachingPositions", newName: "IX_TeachingPositions_deactivated_by_user_id");
            migrationBuilder.RenameIndex(name: "IX_CourseSections_teacher_id", table: "TeachingPositions", newName: "IX_TeachingPositions_teacher_id");
            migrationBuilder.DropColumn(name: "is_annual", table: "TeachingPositions");

            migrationBuilder.RenameColumn(
                name: "course_section_id",
                table: "TeacherAssignments",
                newName: "teaching_position_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherAssignments_course_section_id",
                table: "TeacherAssignments",
                newName: "IX_TeacherAssignments_teaching_position_id");

            migrationBuilder.RenameColumn(
                name: "course_section_id",
                table: "Gradebooks",
                newName: "teaching_position_id");

            migrationBuilder.RenameIndex(
                name: "IX_Gradebooks_course_section_id",
                table: "Gradebooks",
                newName: "IX_Gradebooks_teaching_position_id");

            migrationBuilder.RenameColumn(
                name: "course_section_id",
                table: "Enrollments",
                newName: "teaching_position_id");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_course_section_id",
                table: "Enrollments",
                newName: "IX_Enrollments_teaching_position_id");

            migrationBuilder.RenameColumn(
                name: "course_section_id",
                table: "AttendanceSessions",
                newName: "teaching_position_id");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_course_section_id",
                table: "AttendanceSessions",
                newName: "IX_AttendanceSessions_teaching_position_id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_TeachingPositions_teaching_position_id",
                table: "AttendanceSessions",
                column: "teaching_position_id",
                principalTable: "TeachingPositions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_TeachingPositions_teaching_position_id",
                table: "Enrollments",
                column: "teaching_position_id",
                principalTable: "TeachingPositions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Gradebooks_TeachingPositions_teaching_position_id",
                table: "Gradebooks",
                column: "teaching_position_id",
                principalTable: "TeachingPositions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherAssignments_TeachingPositions_teaching_position_id",
                table: "TeacherAssignments",
                column: "teaching_position_id",
                principalTable: "TeachingPositions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
