using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
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
                    session_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true),
                    scope = table.Column<int>(type: "int", nullable: false),
                    units = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    edit_deadline_utc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_administratively_reopened = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    closed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    closed_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.id);
                    table.CheckConstraint("CK_AttendanceSessions_TimeRange", "([scope] = 0 AND [start_time] IS NOT NULL AND [end_time] IS NOT NULL AND [end_time] > [start_time]) OR ([scope] = 1 AND [start_time] IS NULL AND [end_time] IS NULL AND [units] = 1)");
                    table.CheckConstraint("CK_AttendanceSessions_Units", "[units] >= 1 AND [units] <= 12");
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attendance_session_id = table.Column<long>(type: "bigint", nullable: false),
                    enrollment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AttendanceSessions_attendance_session_id",
                        column: x => x.attendance_session_id,
                        principalTable: "AttendanceSessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "Enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSessionReopenings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attendance_session_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    reopened_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reopened_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessionReopenings", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionReopenings_AttendanceSessions_attendance_session_id",
                        column: x => x.attendance_session_id,
                        principalTable: "AttendanceSessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionReopenings_Users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceJustifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attendance_record_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_status = table.Column<int>(type: "int", nullable: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    evidence_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_current = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceJustifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttendanceJustifications_AttendanceRecords_attendance_record_id",
                        column: x => x.attendance_record_id,
                        principalTable: "AttendanceRecords",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceJustifications_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceJustifications_attendance_record_id",
                table: "AttendanceJustifications",
                column: "attendance_record_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceJustifications_created_by_user_id",
                table: "AttendanceJustifications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_attendance_session_id_enrollment_id",
                table: "AttendanceRecords",
                columns: new[] { "attendance_session_id", "enrollment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_enrollment_id",
                table: "AttendanceRecords",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_student_id_attendance_session_id",
                table: "AttendanceRecords",
                columns: new[] { "student_id", "attendance_session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_updated_by_user_id",
                table: "AttendanceRecords",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionReopenings_attendance_session_id_reopened_at",
                table: "AttendanceSessionReopenings",
                columns: new[] { "attendance_session_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionReopenings_reopened_by_user_id",
                table: "AttendanceSessionReopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_closed_by_user_id",
                table: "AttendanceSessions",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_commission_id",
                table: "AttendanceSessions",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_course_id_commission_id_academic_year_semester_session_date_start_time_scope",
                table: "AttendanceSessions",
                columns: new[] { "course_id", "commission_id", "academic_year", "semester", "session_date", "start_time", "scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_created_by_user_id",
                table: "AttendanceSessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_idempotency_key",
                table: "AttendanceSessions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_teaching_position_id",
                table: "AttendanceSessions",
                column: "teaching_position_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceJustifications");

            migrationBuilder.DropTable(
                name: "AttendanceSessionReopenings");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");
        }
    }
}
