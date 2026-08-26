using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionApplicationStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdmissionApplicationStatusHistory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    admission_application_id = table.Column<long>(type: "bigint", nullable: false),
                    from_status = table.Column<int>(type: "int", nullable: true),
                    to_status = table.Column<int>(type: "int", nullable: false),
                    changed_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    changed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationStatusHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationStatusHistory_AdmissionApplications_admission_application_id",
                        column: x => x.admission_application_id,
                        principalTable: "AdmissionApplications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationStatusHistory_Users_changed_by_user_id",
                        column: x => x.changed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationStatusHistory_admission_application_id_changed_at",
                table: "AdmissionApplicationStatusHistory",
                columns: new[] { "admission_application_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationStatusHistory_changed_by_user_id",
                table: "AdmissionApplicationStatusHistory",
                column: "changed_by_user_id");

            migrationBuilder.Sql("""
                INSERT INTO [AdmissionApplicationStatusHistory]
                    ([admission_application_id], [from_status], [to_status], [changed_at], [changed_by_user_id], [reason])
                SELECT
                    [id], NULL, [status], [created_at], NULL, N'Backfilled from existing admission application.'
                FROM [AdmissionApplications];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionApplicationStatusHistory");
        }
    }
}
