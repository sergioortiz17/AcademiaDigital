using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsAndReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    kind = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    confirmation_idempotency_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    payment_method_id = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    external_reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    confirmation_requested_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    confirmation_requested_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    confirmed_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "[amount] > 0");
                    table.CheckConstraint("CK_Payments_Currency", "[currency] = 'ARS'");
                    table.CheckConstraint("CK_Payments_Status", "[status] >= 0 AND [status] <= 4");
                    table.ForeignKey(
                        name: "FK_Payments_PaymentMethods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_confirmation_requested_by_user_id",
                        column: x => x.confirmation_requested_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_debt_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_Amount", "[amount] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_StudentDebts_student_debt_id",
                        column: x => x.student_debt_id,
                        principalTable: "StudentDebts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    decision = table.Column<int>(type: "int", nullable: false),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliations", x => x.Id);
                    table.CheckConstraint("CK_PaymentReconciliations_Decision", "[decision] IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_PaymentReconciliations_Payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentReconciliations_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReversals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReversals", x => x.Id);
                    table.CheckConstraint("CK_PaymentReversals_Amount", "[amount] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentReversals_Payments_payment_id",
                        column: x => x.payment_id,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentReversals_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PaymentMethods",
                columns: new[] { "Id", "code", "display_order", "is_active", "kind", "name" },
                values: new object[,]
                {
                    { 1, "CASH", 1, true, 0, "Efectivo" },
                    { 2, "BANK_TRANSFER", 2, true, 1, "Transferencia bancaria" },
                    { 3, "DEBIT_CARD", 3, true, 2, "Tarjeta de débito" },
                    { 4, "CREDIT_CARD", 4, true, 3, "Tarjeta de crédito" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_payment_id_student_debt_id",
                table: "PaymentAllocations",
                columns: new[] { "payment_id", "student_debt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_student_debt_id",
                table: "PaymentAllocations",
                column: "student_debt_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_code",
                table: "PaymentMethods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_kind",
                table: "PaymentMethods",
                column: "kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_created_by_user_id",
                table: "PaymentReconciliations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_payment_id",
                table: "PaymentReconciliations",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReversals_created_by_user_id",
                table: "PaymentReversals",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReversals_payment_id",
                table: "PaymentReversals",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReversals_public_id",
                table: "PaymentReversals",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_confirmation_idempotency_key",
                table: "Payments",
                column: "confirmation_idempotency_key",
                unique: true,
                filter: "[confirmation_idempotency_key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_confirmation_requested_by_user_id",
                table: "Payments",
                column: "confirmation_requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_confirmed_by_user_id",
                table: "Payments",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_created_by_user_id",
                table: "Payments",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_payment_method_id",
                table: "Payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_public_id",
                table: "Payments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_status_created_at",
                table: "Payments",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_student_id_created_at",
                table: "Payments",
                columns: new[] { "student_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PaymentReconciliations");

            migrationBuilder.DropTable(
                name: "PaymentReversals");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PaymentMethods");
        }
    }
}
