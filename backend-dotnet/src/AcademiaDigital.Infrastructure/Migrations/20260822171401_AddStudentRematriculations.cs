using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentRematriculations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentRematriculations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: false),
                    career_id = table.Column<int>(type: "int", nullable: false),
                    study_plan_id = table.Column<int>(type: "int", nullable: false),
                    commission_id = table.Column<int>(type: "int", nullable: false),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    year_number = table.Column<int>(type: "int", nullable: false),
                    rematriculated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRematriculations", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_StudentCareers_student_career_id",
                        column: x => x.student_career_id,
                        principalTable: "StudentCareers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_career_id_academic_year",
                table: "StudentRematriculations",
                columns: new[] { "career_id", "academic_year" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_commission_id",
                table: "StudentRematriculations",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_created_by_user_id",
                table: "StudentRematriculations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_student_career_id_academic_year",
                table: "StudentRematriculations",
                columns: new[] { "student_career_id", "academic_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_student_id",
                table: "StudentRematriculations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_study_plan_id",
                table: "StudentRematriculations",
                column: "study_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentRematriculations");
        }
    }
}
