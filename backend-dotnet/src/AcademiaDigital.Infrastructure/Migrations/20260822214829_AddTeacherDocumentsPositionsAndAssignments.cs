using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherDocumentsPositionsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "commission_id",
                table: "TeachingPositions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "TeachingPositions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_at",
                table: "TeachingPositions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "deactivated_by_user_id",
                table: "TeachingPositions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deactivation_reason",
                table: "TeachingPositions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "TeachingPositions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "TeachingPositions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teaching_position_id = table.Column<int>(type: "int", nullable: false),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    started_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ended_on = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "bit", nullable: false),
                    assignment_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    end_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    assigned_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    ended_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ended_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAssignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Users_ended_by_user_id",
                        column: x => x.ended_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherDocuments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDocuments", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherDocuments_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDocuments_Users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                DECLARE @MigrationTimestamp datetime2 = SYSUTCDATETIME();

                UPDATE [TeachingPositions]
                SET [is_vacant] = CASE WHEN [teacher_id] IS NULL THEN 1 ELSE 0 END,
                    [created_at] = @MigrationTimestamp,
                    [updated_at] = @MigrationTimestamp;

                INSERT INTO [TeacherAssignments]
                    ([teaching_position_id], [teacher_id], [started_on], [ended_on], [is_current],
                     [assignment_reason], [end_reason], [assigned_by_user_id], [ended_by_user_id],
                     [created_at], [ended_at])
                SELECT [id], [teacher_id],
                       DATEFROMPARTS(
                           CASE WHEN [academic_year] BETWEEN 1 AND 9999
                                THEN [academic_year] ELSE YEAR(@MigrationTimestamp) END,
                           1, 1),
                       NULL, 1, N'Backfilled from legacy TeachingPositions.teacher_id', NULL,
                       NULL, NULL, @MigrationTimestamp, NULL
                FROM [TeachingPositions]
                WHERE [teacher_id] IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_academic_year_semester_is_active",
                table: "TeachingPositions",
                columns: new[] { "academic_year", "semester", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_commission_id_course_id",
                table: "TeachingPositions",
                columns: new[] { "commission_id", "course_id" });

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_deactivated_by_user_id",
                table: "TeachingPositions",
                column: "deactivated_by_user_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TeachingPositions_AssignmentState",
                table: "TeachingPositions",
                sql: "([is_vacant] = 1 AND [teacher_id] IS NULL) OR ([is_vacant] = 0 AND [teacher_id] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_assigned_by_user_id",
                table: "TeacherAssignments",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_ended_by_user_id",
                table: "TeacherAssignments",
                column: "ended_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_teacher_id_is_current",
                table: "TeacherAssignments",
                columns: new[] { "teacher_id", "is_current" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_teaching_position_id",
                table: "TeacherAssignments",
                column: "teaching_position_id",
                unique: true,
                filter: "[is_current] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_reviewed_by_user_id",
                table: "TeacherDocuments",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_teacher_id_document_type_version",
                table: "TeacherDocuments",
                columns: new[] { "teacher_id", "document_type", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_teacher_id_submitted_at",
                table: "TeacherDocuments",
                columns: new[] { "teacher_id", "submitted_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingPositions_Commissions_commission_id",
                table: "TeachingPositions",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingPositions_Users_deactivated_by_user_id",
                table: "TeachingPositions",
                column: "deactivated_by_user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeachingPositions_Commissions_commission_id",
                table: "TeachingPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingPositions_Users_deactivated_by_user_id",
                table: "TeachingPositions");

            migrationBuilder.DropTable(
                name: "TeacherAssignments");

            migrationBuilder.DropTable(
                name: "TeacherDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TeachingPositions_academic_year_semester_is_active",
                table: "TeachingPositions");

            migrationBuilder.DropIndex(
                name: "IX_TeachingPositions_commission_id_course_id",
                table: "TeachingPositions");

            migrationBuilder.DropIndex(
                name: "IX_TeachingPositions_deactivated_by_user_id",
                table: "TeachingPositions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TeachingPositions_AssignmentState",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "commission_id",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "deactivated_at",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "deactivated_by_user_id",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "deactivation_reason",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "TeachingPositions");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "TeachingPositions");
        }
    }
}
