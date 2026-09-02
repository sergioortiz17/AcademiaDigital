using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionApplicationDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionApplicationDocuments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admission_application_id = table.Column<long>(type: "bigint", nullable: false),
                    document_requirement_id = table.Column<int>(type: "int", nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    observation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationDocuments", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationDocuments_AdmissionApplications_admission_application_id",
                        column: x => x.admission_application_id,
                        principalTable: "AdmissionApplications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationDocuments_DocumentRequirements_document_requirement_id",
                        column: x => x.document_requirement_id,
                        principalTable: "DocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationDocuments_Users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationDocuments_admission_application_id_document_requirement_id_submitted_at",
                table: "AdmissionApplicationDocuments",
                columns: new[] { "admission_application_id", "document_requirement_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationDocuments_document_requirement_id",
                table: "AdmissionApplicationDocuments",
                column: "document_requirement_id");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationDocuments_reviewed_by_user_id",
                table: "AdmissionApplicationDocuments",
                column: "reviewed_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionApplicationDocuments");
        }
    }
}
