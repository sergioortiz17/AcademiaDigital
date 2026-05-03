using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Careers",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    total_credits = table.Column<int>(type: "int", nullable: false),
                    duration_years = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Careers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CooperativeEntities",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    cuit = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    contact_person = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    join_date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CooperativeEntities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    password = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    date_joined = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    credits = table.Column<int>(type: "int", nullable: false),
                    weekly_hours = table.Column<int>(type: "int", nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    career_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.id);
                    table.ForeignKey(
                        name: "FK_Subjects_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActiveSessions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveSessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_ActiveSessions_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administratives",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    position = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    hire_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administratives", x => x.id);
                    table.ForeignKey(
                        name: "FK_Administratives_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Communications",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    published_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    author_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.id);
                    table.ForeignKey(
                        name: "FK_Communications_Users_author_id",
                        column: x => x.author_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    legajo_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    career_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.id);
                    table.ForeignKey(
                        name: "FK_Students_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    employee_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    specialization_area = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    hire_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.id);
                    table.ForeignKey(
                        name: "FK_Teachers_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "TeacherContests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    open_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    close_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    subject_id = table.Column<int>(type: "int", nullable: true),
                    career_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherContests", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherContests_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeacherContests_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TeachingPositions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    position_type = table.Column<int>(type: "int", nullable: false),
                    max_students = table.Column<int>(type: "int", nullable: false),
                    is_vacant = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingPositions", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeachingPositions_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeachingPositions_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ContestApplications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    application_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    contest_id = table.Column<int>(type: "int", nullable: false),
                    applicant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestApplications", x => x.id);
                    table.ForeignKey(
                        name: "FK_ContestApplications_TeacherContests_contest_id",
                        column: x => x.contest_id,
                        principalTable: "TeacherContests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestApplications_Users_applicant_id",
                        column: x => x.applicant_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    final_grade = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    teaching_position_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Subjects_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subjects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_user_id",
                table: "ActiveSessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Administratives_employee_number",
                table: "Administratives",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Administratives_user_id",
                table: "Administratives",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_code",
                table: "Careers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communications_author_id",
                table: "Communications",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContestApplications_applicant_id",
                table: "ContestApplications",
                column: "applicant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContestApplications_contest_id_applicant_id",
                table: "ContestApplications",
                columns: new[] { "contest_id", "applicant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CooperativeEntities_cuit",
                table: "CooperativeEntities",
                column: "cuit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_id_subject_id_academic_year_semester",
                table: "Enrollments",
                columns: new[] { "student_id", "subject_id", "academic_year", "semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_subject_id",
                table: "Enrollments",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_teaching_position_id",
                table: "Enrollments",
                column: "teaching_position_id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_career_id",
                table: "Students",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_legajo_number",
                table: "Students",
                column: "legajo_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_user_id",
                table: "Students",
                column: "user_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContests_career_id",
                table: "TeacherContests",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContests_subject_id",
                table: "TeacherContests",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_employee_number",
                table: "Teachers",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_subject_id",
                table: "TeachingPositions",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_teacher_id",
                table: "TeachingPositions",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveSessions");

            migrationBuilder.DropTable(
                name: "Administratives");

            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropTable(
                name: "ContestApplications");

            migrationBuilder.DropTable(
                name: "CooperativeEntities");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "SubjectPrerequisites");

            migrationBuilder.DropTable(
                name: "TeacherContests");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "TeachingPositions");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Careers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
