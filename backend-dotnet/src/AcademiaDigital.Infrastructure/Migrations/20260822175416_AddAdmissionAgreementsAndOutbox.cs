using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionAgreementsAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionAgreements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admission_application_id = table.Column<long>(type: "bigint", nullable: false),
                    agreement_number = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    storage_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionAgreements", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionAgreements_AdmissionApplications_admission_application_id",
                        column: x => x.admission_application_id,
                        principalTable: "AdmissionApplications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    aggregate_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    deduplication_key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    available_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    processing_started_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    attempts = table.Column<int>(type: "int", nullable: false),
                    last_error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    row_version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAgreements_admission_application_id",
                table: "AdmissionAgreements",
                column: "admission_application_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionAgreements_agreement_number",
                table: "AdmissionAgreements",
                column: "agreement_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_deduplication_key",
                table: "OutboxMessages",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_status_available_at",
                table: "OutboxMessages",
                columns: new[] { "status", "available_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionAgreements");

            migrationBuilder.DropTable(
                name: "OutboxMessages");
        }
    }
}
