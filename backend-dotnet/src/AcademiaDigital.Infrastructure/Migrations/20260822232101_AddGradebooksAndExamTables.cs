using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGradebooksAndExamTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "minimum_final_exam_grade",
                table: "CourseApprovalRules",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 6m);

            migrationBuilder.CreateTable(
                name: "ExamTables",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idempotency_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    call_number = table.Column<int>(type: "int", nullable: false),
                    exam_date_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    registration_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    grading_started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    grading_started_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    published_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTables", x => x.id);
                    table.CheckConstraint("CK_ExamTables_CallNumber", "[call_number] >= 1 AND [call_number] <= 10");
                    table.CheckConstraint("CK_ExamTables_Deadline", "[registration_deadline_utc] <= [exam_date_utc]");
                    table.ForeignKey(
                        name: "FK_ExamTables_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamTables_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamTables_Users_grading_started_by_user_id",
                        column: x => x.grading_started_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamTables_Users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Gradebooks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    idempotency_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    teaching_position_id = table.Column<int>(type: "int", nullable: false),
                    course_id = table.Column<int>(type: "int", nullable: false),
                    commission_id = table.Column<int>(type: "int", nullable: false),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    published_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    published_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    closed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    closed_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gradebooks", x => x.id);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamRegistrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    exam_table_id = table.Column<long>(type: "bigint", nullable: false),
                    enrollment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    attempt_number = table.Column<int>(type: "int", nullable: false),
                    registered_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    registered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_enrollment_status = table.Column<int>(type: "int", nullable: true),
                    previous_final_grade = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    result_applied_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRegistrations", x => x.id);
                    table.CheckConstraint("CK_ExamRegistrations_Attempt", "[attempt_number] >= 1");
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "Enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_ExamTables_exam_table_id",
                        column: x => x.exam_table_id,
                        principalTable: "ExamTables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Users_registered_by_user_id",
                        column: x => x.registered_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamTableReopenings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    exam_table_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    reopened_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reopened_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTableReopenings", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamTableReopenings_ExamTables_exam_table_id",
                        column: x => x.exam_table_id,
                        principalTable: "ExamTables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamTableReopenings_Users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamTribunalMembers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    exam_table_id = table.Column<long>(type: "bigint", nullable: false),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTribunalMembers", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamTribunalMembers_ExamTables_exam_table_id",
                        column: x => x.exam_table_id,
                        principalTable: "ExamTables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamTribunalMembers_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradebookEvaluations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gradebook_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    weight_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    maximum_score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookEvaluations", x => x.id);
                    table.CheckConstraint("CK_GradebookEvaluations_Maximum", "[maximum_score] > 0 AND [maximum_score] <= 100");
                    table.CheckConstraint("CK_GradebookEvaluations_Weight", "[weight_percentage] > 0 AND [weight_percentage] <= 100");
                    table.ForeignKey(
                        name: "FK_GradebookEvaluations_Gradebooks_gradebook_id",
                        column: x => x.gradebook_id,
                        principalTable: "Gradebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradebookReopenings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gradebook_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_status = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    reopened_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reopened_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookReopenings", x => x.id);
                    table.ForeignKey(
                        name: "FK_GradebookReopenings_Gradebooks_gradebook_id",
                        column: x => x.gradebook_id,
                        principalTable: "Gradebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradebookReopenings_Users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamGradeRevisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    exam_registration_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    is_current = table.Column<bool>(type: "bit", nullable: false),
                    outcome = table.Column<int>(type: "int", nullable: false),
                    grade = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamGradeRevisions", x => x.id);
                    table.CheckConstraint("CK_ExamGradeRevisions_Grade", "[grade] IS NULL OR ([grade] >= 0 AND [grade] <= 10)");
                    table.ForeignKey(
                        name: "FK_ExamGradeRevisions_ExamRegistrations_exam_registration_id",
                        column: x => x.exam_registration_id,
                        principalTable: "ExamRegistrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamGradeRevisions_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeEntryRevisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    gradebook_id = table.Column<long>(type: "bigint", nullable: false),
                    evaluation_id = table.Column<long>(type: "bigint", nullable: false),
                    enrollment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    is_current = table.Column<bool>(type: "bit", nullable: false),
                    score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeEntryRevisions", x => x.id);
                    table.CheckConstraint("CK_GradeEntryRevisions_Score", "[score] >= 0 AND [score] <= 100");
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "Enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_GradebookEvaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "GradebookEvaluations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Gradebooks_gradebook_id",
                        column: x => x.gradebook_id,
                        principalTable: "Gradebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseApprovalRules_FinalExamGrade",
                table: "CourseApprovalRules",
                sql: "[minimum_final_exam_grade] >= 1 AND [minimum_final_exam_grade] <= 10");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGradeRevisions_created_by_user_id",
                table: "ExamGradeRevisions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGradeRevisions_exam_registration_id",
                table: "ExamGradeRevisions",
                column: "exam_registration_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGradeRevisions_exam_registration_id_version",
                table: "ExamGradeRevisions",
                columns: new[] { "exam_registration_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_enrollment_id_attempt_number",
                table: "ExamRegistrations",
                columns: new[] { "enrollment_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_exam_table_id_enrollment_id",
                table: "ExamRegistrations",
                columns: new[] { "exam_table_id", "enrollment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_registered_by_user_id",
                table: "ExamRegistrations",
                column: "registered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_student_id",
                table: "ExamRegistrations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTableReopenings_exam_table_id_reopened_at",
                table: "ExamTableReopenings",
                columns: new[] { "exam_table_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamTableReopenings_reopened_by_user_id",
                table: "ExamTableReopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_course_id_exam_date_utc_call_number",
                table: "ExamTables",
                columns: new[] { "course_id", "exam_date_utc", "call_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_created_by_user_id",
                table: "ExamTables",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_grading_started_by_user_id",
                table: "ExamTables",
                column: "grading_started_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_idempotency_key",
                table: "ExamTables",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_published_by_user_id",
                table: "ExamTables",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTribunalMembers_exam_table_id_role",
                table: "ExamTribunalMembers",
                columns: new[] { "exam_table_id", "role" },
                unique: true,
                filter: "[role] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTribunalMembers_exam_table_id_teacher_id",
                table: "ExamTribunalMembers",
                columns: new[] { "exam_table_id", "teacher_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamTribunalMembers_teacher_id",
                table: "ExamTribunalMembers",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradebookEvaluations_gradebook_id_display_order",
                table: "GradebookEvaluations",
                columns: new[] { "gradebook_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradebookEvaluations_gradebook_id_name",
                table: "GradebookEvaluations",
                columns: new[] { "gradebook_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradebookReopenings_gradebook_id_reopened_at",
                table: "GradebookReopenings",
                columns: new[] { "gradebook_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_GradebookReopenings_reopened_by_user_id",
                table: "GradebookReopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_approved_by_user_id",
                table: "Gradebooks",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_closed_by_user_id",
                table: "Gradebooks",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_commission_id",
                table: "Gradebooks",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_course_id_commission_id_academic_year_semester",
                table: "Gradebooks",
                columns: new[] { "course_id", "commission_id", "academic_year", "semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_created_by_user_id",
                table: "Gradebooks",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_idempotency_key",
                table: "Gradebooks",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_published_by_user_id",
                table: "Gradebooks",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_submitted_by_user_id",
                table: "Gradebooks",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_teaching_position_id",
                table: "Gradebooks",
                column: "teaching_position_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_created_by_user_id",
                table: "GradeEntryRevisions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_enrollment_id",
                table: "GradeEntryRevisions",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_evaluation_id_enrollment_id",
                table: "GradeEntryRevisions",
                columns: new[] { "evaluation_id", "enrollment_id" },
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_evaluation_id_enrollment_id_version",
                table: "GradeEntryRevisions",
                columns: new[] { "evaluation_id", "enrollment_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_gradebook_id_student_id",
                table: "GradeEntryRevisions",
                columns: new[] { "gradebook_id", "student_id" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_student_id",
                table: "GradeEntryRevisions",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExamGradeRevisions");

            migrationBuilder.DropTable(
                name: "ExamTableReopenings");

            migrationBuilder.DropTable(
                name: "ExamTribunalMembers");

            migrationBuilder.DropTable(
                name: "GradebookReopenings");

            migrationBuilder.DropTable(
                name: "GradeEntryRevisions");

            migrationBuilder.DropTable(
                name: "ExamRegistrations");

            migrationBuilder.DropTable(
                name: "GradebookEvaluations");

            migrationBuilder.DropTable(
                name: "ExamTables");

            migrationBuilder.DropTable(
                name: "Gradebooks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseApprovalRules_FinalExamGrade",
                table: "CourseApprovalRules");

            migrationBuilder.DropColumn(
                name: "minimum_final_exam_grade",
                table: "CourseApprovalRules");
        }
    }
}
