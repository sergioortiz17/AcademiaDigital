using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AcademicStudyPlansCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Subjects_subject_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectPrerequisites_Subjects_prerequisite_subject_id",
                table: "SubjectPrerequisites");

            migrationBuilder.DropForeignKey(
                name: "FK_SubjectPrerequisites_Subjects_subject_id",
                table: "SubjectPrerequisites");

            migrationBuilder.DropForeignKey(
                name: "FK_Subjects_Careers_career_id",
                table: "Subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherContests_Subjects_subject_id",
                table: "TeacherContests");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingPositions_Subjects_subject_id",
                table: "TeachingPositions");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_career_id",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Subjects_code",
                table: "Subjects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects");

            migrationBuilder.RenameTable(
                name: "Subjects",
                newName: "Courses");

            migrationBuilder.RenameColumn(
                name: "subject_id",
                table: "TeachingPositions",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeachingPositions_subject_id",
                table: "TeachingPositions",
                newName: "IX_TeachingPositions_course_id");

            migrationBuilder.RenameColumn(
                name: "subject_id",
                table: "TeacherContests",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherContests_subject_id",
                table: "TeacherContests",
                newName: "IX_TeacherContests_course_id");

            migrationBuilder.RenameColumn(
                name: "subject_id",
                table: "Enrollments",
                newName: "course_id");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_subject_id",
                table: "Enrollments",
                newName: "IX_Enrollments_course_id");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_student_id_subject_id_academic_year_semester",
                table: "Enrollments",
                newName: "IX_Enrollments_student_id_course_id_academic_year_semester");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Courses",
                table: "Courses",
                column: "id");

            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "Users",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "study_plan_course_id",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "Careers",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Careers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Careers] SET [updated_at] = SYSUTCDATETIME() WHERE [updated_at] IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "Careers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                table: "Courses",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Courses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Courses] SET [created_at] = SYSUTCDATETIME() WHERE [created_at] IS NULL");

            migrationBuilder.Sql(
                "UPDATE [Courses] SET [updated_at] = SYSUTCDATETIME() WHERE [updated_at] IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "Courses",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "Courses",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "CourseTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlans",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    career_id = table.Column<int>(type: "int", nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    version_number = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlans", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudyPlans_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    study_plan_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    prerequisite_course_id = table.Column<int>(type: "int", nullable: false),
                    prerequisite_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    minimum_required_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => x.id);
                    table.CheckConstraint("CK_CoursePrerequisites_NoSelfReference", "course_id <> prerequisite_course_id");
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_prerequisite_course_id",
                        column: x => x.prerequisite_course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentStudyPlans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    study_plan_id = table.Column<int>(type: "int", nullable: false),
                    is_current = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    assigned_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    migration_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentStudyPlans", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentStudyPlans_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentStudyPlans_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlanCourses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    study_plan_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    year_number = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    course_type_id = table.Column<int>(type: "int", nullable: true),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_mandatory = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    credits = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    workload_hours = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlanCourses", x => x.id);
                    table.CheckConstraint("CK_StudyPlanCourses_Semester", "semester IN (1, 2)");
                    table.CheckConstraint("CK_StudyPlanCourses_YearNumber", "year_number > 0");
                    table.ForeignKey(
                        name: "FK_StudyPlanCourses_CourseTypes_course_type_id",
                        column: x => x.course_type_id,
                        principalTable: "CourseTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyPlanCourses_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyPlanCourses_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseApprovalRules",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    study_plan_course_id = table.Column<int>(type: "int", nullable: false),
                    minimum_regular_grade = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    minimum_promotion_grade = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    minimum_attendance_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    requires_final_exam = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    allows_promotion = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    policy_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseApprovalRules", x => x.id);
                    table.ForeignKey(
                        name: "FK_CourseApprovalRules_StudyPlanCourses_study_plan_course_id",
                        column: x => x.study_plan_course_id,
                        principalTable: "StudyPlanCourses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_id_status",
                table: "Enrollments",
                columns: new[] { "student_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_study_plan_course_id_status",
                table: "Enrollments",
                columns: new[] { "study_plan_course_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseApprovalRules_study_plan_course_id",
                table: "CourseApprovalRules",
                column: "study_plan_course_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_course_id",
                table: "CoursePrerequisites",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_prerequisite_course_id",
                table: "CoursePrerequisites",
                column: "prerequisite_course_id");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_study_plan_id_course_id_prerequisite_course_id",
                table: "CoursePrerequisites",
                columns: new[] { "study_plan_id", "course_id", "prerequisite_course_id" },
                unique: true,
                filter: "[is_active] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_career_id_code",
                table: "Courses",
                columns: new[] { "career_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseTypes_code",
                table: "CourseTypes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_student_id",
                table: "StudentStudyPlans",
                column: "student_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_study_plan_id",
                table: "StudentStudyPlans",
                column: "study_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_course_id",
                table: "StudyPlanCourses",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_course_type_id",
                table: "StudyPlanCourses",
                column: "course_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_study_plan_id_course_id",
                table: "StudyPlanCourses",
                columns: new[] { "study_plan_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_study_plan_id_year_number_semester_sort_order",
                table: "StudyPlanCourses",
                columns: new[] { "study_plan_id", "year_number", "semester", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_career_id_version_number",
                table: "StudyPlans",
                columns: new[] { "career_id", "version_number" },
                unique: true);

            migrationBuilder.Sql(@"
INSERT INTO [StudyPlans] ([career_id], [code], [name], [version_number], [status], [effective_from], [effective_to], [is_active], [created_at], [updated_at])
SELECT
    c.[id],
    LEFT(CONCAT(N'LEGACY-', c.[id]), 20),
    LEFT(CONCAT(c.[name], N' Plan 1'), 200),
    1,
    N'Active',
    NULL,
    NULL,
    CAST(1 AS bit),
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM [Careers] c
WHERE NOT EXISTS (
    SELECT 1
    FROM [StudyPlans] sp
    WHERE sp.[career_id] = c.[id]
      AND sp.[version_number] = 1
);");

            migrationBuilder.Sql(@"
;WITH LegacyCourses AS
(
    SELECT
        sp.[id] AS [study_plan_id],
        c.[id] AS [course_id],
        CASE WHEN c.[year] > 0 THEN c.[year] ELSE 1 END AS [year_number],
        CASE WHEN c.[semester] IN (1, 2) THEN c.[semester] ELSE 1 END AS [semester],
        c.[credits],
        c.[weekly_hours],
        c.[is_active],
        ROW_NUMBER() OVER (
            PARTITION BY
                sp.[id],
                CASE WHEN c.[year] > 0 THEN c.[year] ELSE 1 END,
                CASE WHEN c.[semester] IN (1, 2) THEN c.[semester] ELSE 1 END
            ORDER BY
                CASE WHEN c.[year] > 0 THEN c.[year] ELSE 1 END,
                CASE WHEN c.[semester] IN (1, 2) THEN c.[semester] ELSE 1 END,
                c.[code],
                c.[id]
        ) AS [sort_order]
    FROM [Courses] c
    INNER JOIN [StudyPlans] sp
        ON sp.[career_id] = c.[career_id]
       AND sp.[version_number] = 1
)
INSERT INTO [StudyPlanCourses] ([study_plan_id], [course_id], [year_number], [semester], [course_type_id], [sort_order], [is_mandatory], [credits], [workload_hours], [is_active], [created_at], [updated_at])
SELECT
    lc.[study_plan_id],
    lc.[course_id],
    lc.[year_number],
    lc.[semester],
    NULL,
    lc.[sort_order],
    CAST(1 AS bit),
    CASE WHEN lc.[credits] BETWEEN 0 AND 999 THEN CONVERT(decimal(5, 2), lc.[credits]) ELSE NULL END,
    lc.[weekly_hours],
    lc.[is_active],
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM LegacyCourses lc
WHERE NOT EXISTS (
    SELECT 1
    FROM [StudyPlanCourses] spc
    WHERE spc.[study_plan_id] = lc.[study_plan_id]
      AND spc.[course_id] = lc.[course_id]
);");

            migrationBuilder.Sql(@"
INSERT INTO [CoursePrerequisites] ([study_plan_id], [course_id], [prerequisite_course_id], [prerequisite_type], [minimum_required_status], [is_active], [created_at], [updated_at])
SELECT
    sp.[id],
    spr.[subject_id],
    spr.[prerequisite_subject_id],
    N'Strict',
    N'Approved',
    CAST(1 AS bit),
    SYSUTCDATETIME(),
    SYSUTCDATETIME()
FROM [SubjectPrerequisites] spr
INNER JOIN [Courses] c
    ON c.[id] = spr.[subject_id]
INNER JOIN [Courses] pc
    ON pc.[id] = spr.[prerequisite_subject_id]
INNER JOIN [StudyPlans] sp
    ON sp.[career_id] = c.[career_id]
   AND sp.[version_number] = 1
WHERE spr.[subject_id] <> spr.[prerequisite_subject_id]
  AND NOT EXISTS (
      SELECT 1
      FROM [CoursePrerequisites] cp
      WHERE cp.[study_plan_id] = sp.[id]
        AND cp.[course_id] = spr.[subject_id]
        AND cp.[prerequisite_course_id] = spr.[prerequisite_subject_id]
  );");

            migrationBuilder.Sql(@"
UPDATE e
SET [study_plan_course_id] = spc.[id]
FROM [Enrollments] e
INNER JOIN [StudyPlanCourses] spc
    ON spc.[course_id] = e.[course_id]
WHERE e.[study_plan_course_id] IS NULL;");

            migrationBuilder.DropTable(
                name: "SubjectPrerequisites");

            migrationBuilder.DropColumn(
                name: "credits",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "semester",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "weekly_hours",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "year",
                table: "Courses");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Careers_career_id",
                table: "Courses",
                column: "career_id",
                principalTable: "Careers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Courses_course_id",
                table: "Enrollments",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_StudyPlanCourses_study_plan_course_id",
                table: "Enrollments",
                column: "study_plan_course_id",
                principalTable: "StudyPlanCourses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherContests_Courses_course_id",
                table: "TeacherContests",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingPositions_Courses_course_id",
                table: "TeachingPositions",
                column: "course_id",
                principalTable: "Courses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Careers_career_id",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Courses_course_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_StudyPlanCourses_study_plan_course_id",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherContests_Courses_course_id",
                table: "TeacherContests");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingPositions_Courses_course_id",
                table: "TeachingPositions");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_student_id_status",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_study_plan_course_id_status",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "study_plan_course_id",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Careers");

            migrationBuilder.AddColumn<int>(
                name: "credits",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "semester",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "weekly_hours",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "year",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
;WITH FirstPlanCourse AS
(
    SELECT
        spc.[course_id],
        spc.[year_number],
        spc.[semester],
        spc.[credits],
        spc.[workload_hours],
        ROW_NUMBER() OVER (
            PARTITION BY spc.[course_id]
            ORDER BY sp.[version_number], spc.[sort_order], spc.[id]
        ) AS [rn]
    FROM [StudyPlanCourses] spc
    INNER JOIN [StudyPlans] sp
        ON sp.[id] = spc.[study_plan_id]
)
UPDATE c
SET
    [year] = fpc.[year_number],
    [semester] = fpc.[semester],
    [credits] = COALESCE(TRY_CONVERT(int, fpc.[credits]), 0),
    [weekly_hours] = COALESCE(fpc.[workload_hours], 0)
FROM [Courses] c
INNER JOIN FirstPlanCourse fpc
    ON fpc.[course_id] = c.[id]
   AND fpc.[rn] = 1;");

            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS [__LegacySubjectPrerequisites];

SELECT
    cp.[course_id] AS [subject_id],
    cp.[prerequisite_course_id] AS [prerequisite_subject_id]
INTO [__LegacySubjectPrerequisites]
FROM [CoursePrerequisites] cp
WHERE cp.[is_active] = 1
GROUP BY cp.[course_id], cp.[prerequisite_course_id];");

            migrationBuilder.DropTable(
                name: "CourseApprovalRules");

            migrationBuilder.DropTable(
                name: "CoursePrerequisites");

            migrationBuilder.DropTable(
                name: "StudentStudyPlans");

            migrationBuilder.DropTable(
                name: "StudyPlanCourses");

            migrationBuilder.DropTable(
                name: "CourseTypes");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropIndex(
                name: "IX_Courses_career_id_code",
                table: "Courses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Courses",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "row_version",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Courses");

            migrationBuilder.RenameTable(
                name: "Courses",
                newName: "Subjects");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "TeachingPositions",
                newName: "subject_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeachingPositions_course_id",
                table: "TeachingPositions",
                newName: "IX_TeachingPositions_subject_id");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "TeacherContests",
                newName: "subject_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeacherContests_course_id",
                table: "TeacherContests",
                newName: "IX_TeacherContests_subject_id");

            migrationBuilder.RenameColumn(
                name: "course_id",
                table: "Enrollments",
                newName: "subject_id");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_student_id_course_id_academic_year_semester",
                table: "Enrollments",
                newName: "IX_Enrollments_student_id_subject_id_academic_year_semester");

            migrationBuilder.RenameIndex(
                name: "IX_Enrollments_course_id",
                table: "Enrollments",
                newName: "IX_Enrollments_subject_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Subjects",
                table: "Subjects",
                column: "id");

            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "SubjectPrerequisites",
                columns: table => new
                {
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    prerequisite_subject_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectPrerequisites", x => new { x.subject_id, x.prerequisite_subject_id });
                    table.ForeignKey(
                        name: "FK_SubjectPrerequisites_Subjects_prerequisite_subject_id",
                        column: x => x.prerequisite_subject_id,
                        principalTable: "Subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectPrerequisites_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
INSERT INTO [SubjectPrerequisites] ([subject_id], [prerequisite_subject_id])
SELECT
    lsp.[subject_id],
    lsp.[prerequisite_subject_id]
FROM [__LegacySubjectPrerequisites] lsp
WHERE EXISTS (
    SELECT 1 FROM [Subjects] s WHERE s.[id] = lsp.[subject_id]
)
AND EXISTS (
    SELECT 1 FROM [Subjects] ps WHERE ps.[id] = lsp.[prerequisite_subject_id]
);");

            migrationBuilder.Sql(
                "DROP TABLE IF EXISTS [__LegacySubjectPrerequisites];");

            migrationBuilder.CreateIndex(
                name: "IX_SubjectPrerequisites_prerequisite_subject_id",
                table: "SubjectPrerequisites",
                column: "prerequisite_subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_career_id",
                table: "Subjects",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_code",
                table: "Subjects",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Subjects_subject_id",
                table: "Enrollments",
                column: "subject_id",
                principalTable: "Subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Subjects_Careers_career_id",
                table: "Subjects",
                column: "career_id",
                principalTable: "Careers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherContests_Subjects_subject_id",
                table: "TeacherContests",
                column: "subject_id",
                principalTable: "Subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingPositions_Subjects_subject_id",
                table: "TeachingPositions",
                column: "subject_id",
                principalTable: "Subjects",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
