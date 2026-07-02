using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "enrollment_period_id",
                table: "Enrollments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shift",
                table: "Enrollments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EnrollmentPeriods",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    career_id = table.Column<int>(type: "int", nullable: false),
                    study_plan_id = table.Column<int>(type: "int", nullable: false),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    semester = table.Column<int>(type: "int", nullable: false),
                    quotas_afternoon = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    quotas_evening = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentPeriods", x => x.id);
                    table.ForeignKey(
                        name: "FK_EnrollmentPeriods_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentPeriods_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_enrollment_period_id",
                table: "Enrollments",
                column: "enrollment_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPeriods_career_id",
                table: "EnrollmentPeriods",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPeriods_study_plan_id",
                table: "EnrollmentPeriods",
                column: "study_plan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_EnrollmentPeriods_enrollment_period_id",
                table: "Enrollments",
                column: "enrollment_period_id",
                principalTable: "EnrollmentPeriods",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_EnrollmentPeriods_enrollment_period_id",
                table: "Enrollments");

            migrationBuilder.DropTable(
                name: "EnrollmentPeriods");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_enrollment_period_id",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "enrollment_period_id",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "shift",
                table: "Enrollments");
        }
    }
}
