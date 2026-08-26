using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDigitalReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Receipts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    receipt_number = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    fiscal_cae = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    fiscal_qr_data = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    issued_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.CheckConstraint("CK_Receipts_Sequence", "[sequence_number] > 0");
                    table.CheckConstraint("CK_Receipts_Status", "[status] >= 0 AND [status] <= 2");
                    table.ForeignKey(
                        name: "FK_Receipts_Payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Receipts_Users_issued_by_user_id",
                        column: x => x.issued_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptSequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequences", x => x.id);
                    table.CheckConstraint("CK_ReceiptSequences_Singleton", "[id] = 1 AND [last_value] >= 0");
                });

            migrationBuilder.InsertData(
                table: "ReceiptSequences",
                columns: new[] { "id", "last_value" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_issued_by_user_id",
                table: "Receipts",
                column: "issued_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_payment_id",
                table: "Receipts",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_public_id",
                table: "Receipts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_receipt_number",
                table: "Receipts",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_sequence_number",
                table: "Receipts",
                column: "sequence_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_status_created_at",
                table: "Receipts",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "ReceiptSequences");
        }
    }
}
