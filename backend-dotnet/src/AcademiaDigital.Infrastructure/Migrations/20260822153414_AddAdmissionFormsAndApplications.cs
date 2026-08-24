using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionFormsAndApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionForms",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    career_id = table.Column<int>(type: "int", nullable: false),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    terms_text = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    reservation_hours = table.Column<int>(type: "int", nullable: false, defaultValue: 72),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionForms", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionForms_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    admission_form_id = table.Column<int>(type: "int", nullable: false),
                    applicant_email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    applicant_dni = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    submitted_fields_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    terms_accepted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reservation_expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplications", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplications_AdmissionForms_admission_form_id",
                        column: x => x.admission_form_id,
                        principalTable: "AdmissionForms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionFormFields",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admission_form_id = table.Column<int>(type: "int", nullable: false),
                    key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    type = table.Column<int>(type: "int", nullable: false),
                    is_required = table.Column<bool>(type: "bit", nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionFormFields", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionFormFields_AdmissionForms_admission_form_id",
                        column: x => x.admission_form_id,
                        principalTable: "AdmissionForms",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_admission_form_id_applicant_dni",
                table: "AdmissionApplications",
                columns: new[] { "admission_form_id", "applicant_dni" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_admission_form_id_applicant_email",
                table: "AdmissionApplications",
                columns: new[] { "admission_form_id", "applicant_email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_public_id",
                table: "AdmissionApplications",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_status_reservation_expires_at",
                table: "AdmissionApplications",
                columns: new[] { "status", "reservation_expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionFormFields_admission_form_id_key",
                table: "AdmissionFormFields",
                columns: new[] { "admission_form_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionForms_career_id",
                table: "AdmissionForms",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionForms_slug",
                table: "AdmissionForms",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionApplications");

            migrationBuilder.DropTable(
                name: "AdmissionFormFields");

            migrationBuilder.DropTable(
                name: "AdmissionForms");
        }
    }
}
