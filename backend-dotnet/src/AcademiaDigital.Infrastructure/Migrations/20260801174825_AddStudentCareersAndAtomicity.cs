using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentCareersAndAtomicity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Students GROUP BY user_id HAVING COUNT(*) > 1)
                    THROW 51000, 'Cannot migrate: more than one Student exists for the same User.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_StudentStudyPlans_student_id",
                table: "StudentStudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_Students_user_id",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentAcademicAssignments_StudentId",
                table: "StudentAcademicAssignments");

            migrationBuilder.AddColumn<long>(
                name: "student_career_id",
                table: "StudentStudyPlans",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StudentCareerId",
                table: "StudentAcademicAssignments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "student_career_id",
                table: "Enrollments",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentCareers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCareers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCareers_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentCareers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                ;WITH Memberships AS
                (
                    SELECT s.id AS StudentId, s.career_id AS CareerId FROM Students s
                    UNION
                    SELECT a.StudentId, a.CareerId FROM StudentAcademicAssignments a
                    UNION
                    SELECT sp.student_id, p.career_id
                    FROM StudentStudyPlans sp
                    INNER JOIN StudyPlans p ON p.id = sp.study_plan_id
                    UNION
                    SELECT e.student_id, COALESCE(ep.career_id, c.career_id) AS CareerId
                    FROM Enrollments e
                    INNER JOIN Courses c ON c.id = e.course_id
                    LEFT JOIN EnrollmentPeriods ep ON ep.id = e.enrollment_period_id
                )
                INSERT INTO StudentCareers (StudentId, CareerId, EnrollmentDate, IsActive, CreatedAt, UpdatedAt)
                SELECT DISTINCT m.StudentId, m.CareerId, s.enrollment_date, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
                FROM Memberships m
                INNER JOIN Students s ON s.id = m.StudentId;

                UPDATE sp
                SET student_career_id = sc.Id
                FROM StudentStudyPlans sp
                INNER JOIN StudyPlans p ON p.id = sp.study_plan_id
                INNER JOIN StudentCareers sc ON sc.StudentId = sp.student_id AND sc.CareerId = p.career_id;

                UPDATE a
                SET StudentCareerId = sc.Id
                FROM StudentAcademicAssignments a
                INNER JOIN StudentCareers sc ON sc.StudentId = a.StudentId AND sc.CareerId = a.CareerId;

                UPDATE e
                SET student_career_id = sc.Id
                FROM Enrollments e
                INNER JOIN Courses c ON c.id = e.course_id
                LEFT JOIN EnrollmentPeriods ep ON ep.id = e.enrollment_period_id
                INNER JOIN StudentCareers sc ON sc.StudentId = e.student_id
                    AND sc.CareerId = COALESCE(ep.career_id, c.career_id);

                IF EXISTS (SELECT 1 FROM StudentStudyPlans WHERE student_career_id IS NULL)
                    THROW 51001, 'Cannot migrate: a StudentStudyPlan cannot be associated with a student career.', 1;
                IF EXISTS (SELECT 1 FROM StudentAcademicAssignments WHERE StudentCareerId IS NULL)
                    THROW 51002, 'Cannot migrate: an academic assignment cannot be associated with a student career.', 1;
                IF EXISTS (SELECT 1 FROM Enrollments WHERE student_career_id IS NULL)
                    THROW 51003, 'Cannot migrate: an enrollment cannot be associated with a student career.', 1;
                """);

            migrationBuilder.AlterColumn<long>(
                name: "student_career_id",
                table: "StudentStudyPlans",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "StudentCareerId",
                table: "StudentAcademicAssignments",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "student_career_id",
                table: "Enrollments",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_student_career_id",
                table: "StudentStudyPlans",
                column: "student_career_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_student_id",
                table: "StudentStudyPlans",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_user_id",
                table: "Students",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudentCareerId",
                table: "StudentAcademicAssignments",
                column: "StudentCareerId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_career_id",
                table: "Enrollments",
                column: "student_career_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCareers_CareerId_IsActive",
                table: "StudentCareers",
                columns: new[] { "CareerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCareers_StudentId_CareerId",
                table: "StudentCareers",
                columns: new[] { "StudentId", "CareerId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_StudentCareers_student_career_id",
                table: "Enrollments",
                column: "student_career_id",
                principalTable: "StudentCareers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAcademicAssignments_StudentCareers_StudentCareerId",
                table: "StudentAcademicAssignments",
                column: "StudentCareerId",
                principalTable: "StudentCareers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentStudyPlans_StudentCareers_student_career_id",
                table: "StudentStudyPlans",
                column: "student_career_id",
                principalTable: "StudentCareers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_StudentCareers_student_career_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentAcademicAssignments_StudentCareers_StudentCareerId",
                table: "StudentAcademicAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentStudyPlans_StudentCareers_student_career_id",
                table: "StudentStudyPlans");

            migrationBuilder.DropTable(
                name: "StudentCareers");

            migrationBuilder.DropIndex(
                name: "IX_StudentStudyPlans_student_career_id",
                table: "StudentStudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_StudentStudyPlans_student_id",
                table: "StudentStudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_Students_user_id",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentAcademicAssignments_StudentCareerId",
                table: "StudentAcademicAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_student_career_id",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "student_career_id",
                table: "StudentStudyPlans");

            migrationBuilder.DropColumn(
                name: "StudentCareerId",
                table: "StudentAcademicAssignments");

            migrationBuilder.DropColumn(
                name: "student_career_id",
                table: "Enrollments");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_student_id",
                table: "StudentStudyPlans",
                column: "student_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_user_id",
                table: "Students",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudentId",
                table: "StudentAcademicAssignments",
                column: "StudentId",
                unique: true,
                filter: "[IsCurrent] = 1");
        }
    }
}
