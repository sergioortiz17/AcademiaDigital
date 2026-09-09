using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AcademiaDigital.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "BillingPlans",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialBenefits",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    scholarship_id = table.Column<int>(type: "integer", nullable: true),
                    career_id = table.Column<int>(type: "integer", nullable: true),
                    student_condition = table.Column<int>(type: "integer", nullable: true),
                    percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialBenefits", x => x.Id);
                    table.CheckConstraint("CK_FinancialBenefits_Percentage", "percentage > 0 AND percentage <= 100");
                    table.CheckConstraint("CK_FinancialBenefits_Scholarship", "(kind = 0 AND scholarship_id IS NULL) OR (kind = 1 AND scholarship_id IS NOT NULL)");
                    table.CheckConstraint("CK_FinancialBenefits_Validity", "valid_from IS NULL OR valid_to IS NULL OR valid_to >= valid_from");
                });

            migrationBuilder.CreateTable(
                name: "FinancialConcepts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialConcepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReceiptSequences",
                schema: "finance",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptSequences", x => x.id);
                    table.CheckConstraint("CK_ReceiptSequences_Singleton", "id = 1 AND last_value >= 0");
                });

            migrationBuilder.CreateTable(
                name: "DebtGenerationBatches",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    billing_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generated_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    generated_debt_count = table.Column<int>(type: "integer", nullable: false),
                    generated_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtGenerationBatches", x => x.Id);
                    table.CheckConstraint("CK_DebtGenerationBatches_Count", "generated_debt_count >= 0");
                    table.CheckConstraint("CK_DebtGenerationBatches_Total", "generated_total >= 0");
                    table.ForeignKey(
                        name: "FK_DebtGenerationBatches_BillingPlans_billing_plan_id",
                        column: x => x.billing_plan_id,
                        principalSchema: "finance",
                        principalTable: "BillingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingPlanItems",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    billing_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    financial_concept_id = table.Column<int>(type: "integer", nullable: false),
                    installment_number = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlanItems", x => x.Id);
                    table.CheckConstraint("CK_BillingPlanItems_Installment", "installment_number > 0");
                    table.ForeignKey(
                        name: "FK_BillingPlanItems_BillingPlans_billing_plan_id",
                        column: x => x.billing_plan_id,
                        principalSchema: "finance",
                        principalTable: "BillingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingPlanItems_FinancialConcepts_financial_concept_id",
                        column: x => x.financial_concept_id,
                        principalSchema: "finance",
                        principalTable: "FinancialConcepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialRates",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    financial_concept_id = table.Column<int>(type: "integer", nullable: false),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    student_condition = table.Column<int>(type: "integer", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    surcharge_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialRates", x => x.Id);
                    table.CheckConstraint("CK_FinancialRates_Amount", "amount > 0");
                    table.CheckConstraint("CK_FinancialRates_Surcharge", "surcharge_percentage >= 0 AND surcharge_percentage <= 100");
                    table.ForeignKey(
                        name: "FK_FinancialRates_FinancialConcepts_financial_concept_id",
                        column: x => x.financial_concept_id,
                        principalSchema: "finance",
                        principalTable: "FinancialConcepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmation_idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    student_dni = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_method_id = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    external_reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    confirmation_requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmation_requested_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "amount > 0");
                    table.CheckConstraint("CK_Payments_Currency", "currency = 'ARS'");
                    table.CheckConstraint("CK_Payments_Status", "status >= 0 AND status <= 4");
                    table.ForeignKey(
                        name: "FK_Payments_PaymentMethods_payment_method_id",
                        column: x => x.payment_method_id,
                        principalSchema: "finance",
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentDebts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    debt_generation_batch_id = table.Column<long>(type: "bigint", nullable: false),
                    billing_plan_item_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: false),
                    financial_concept_id = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    base_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    surcharge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    financial_rate_id = table.Column<long>(type: "bigint", nullable: false),
                    applied_benefit_id = table.Column<long>(type: "bigint", nullable: true),
                    calculation_snapshot_json = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDebts", x => x.Id);
                    table.CheckConstraint("CK_StudentDebts_Amounts", "base_amount > 0 AND surcharge_amount >= 0 AND discount_amount >= 0 AND total_amount >= 0 AND paid_amount >= 0 AND paid_amount <= total_amount");
                    table.CheckConstraint("CK_StudentDebts_Currency", "currency = 'ARS'");
                    table.ForeignKey(
                        name: "FK_StudentDebts_BillingPlanItems_billing_plan_item_id",
                        column: x => x.billing_plan_item_id,
                        principalSchema: "finance",
                        principalTable: "BillingPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_DebtGenerationBatches_debt_generation_batch_id",
                        column: x => x.debt_generation_batch_id,
                        principalSchema: "finance",
                        principalTable: "DebtGenerationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_FinancialBenefits_applied_benefit_id",
                        column: x => x.applied_benefit_id,
                        principalSchema: "finance",
                        principalTable: "FinancialBenefits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_FinancialConcepts_financial_concept_id",
                        column: x => x.financial_concept_id,
                        principalSchema: "finance",
                        principalTable: "FinancialConcepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_FinancialRates_financial_rate_id",
                        column: x => x.financial_rate_id,
                        principalSchema: "finance",
                        principalTable: "FinancialRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    decision = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReconciliations", x => x.Id);
                    table.CheckConstraint("CK_PaymentReconciliations_Decision", "decision IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_PaymentReconciliations_Payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReversals",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReversals", x => x.Id);
                    table.CheckConstraint("CK_PaymentReversals_Amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_PaymentReversals_Payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Receipts",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    fiscal_cae = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    fiscal_qr_data = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receipts", x => x.Id);
                    table.CheckConstraint("CK_Receipts_Sequence", "sequence_number > 0");
                    table.CheckConstraint("CK_Receipts_Status", "status >= 0 AND status <= 2");
                    table.ForeignKey(
                        name: "FK_Receipts_Payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    payment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_debt_id = table.Column<long>(type: "bigint", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_Amount", "amount > 0");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_payment_id",
                        column: x => x.payment_id,
                        principalSchema: "finance",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_StudentDebts_student_debt_id",
                        column: x => x.student_debt_id,
                        principalSchema: "finance",
                        principalTable: "StudentDebts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "finance",
                table: "PaymentMethods",
                columns: new[] { "Id", "code", "display_order", "is_active", "kind", "name" },
                values: new object[,]
                {
                    { 1, "CASH", 1, true, 0, "Efectivo" },
                    { 2, "BANK_TRANSFER", 2, true, 1, "Transferencia bancaria" },
                    { 3, "DEBIT_CARD", 3, true, 2, "Tarjeta de débito" },
                    { 4, "CREDIT_CARD", 4, true, 3, "Tarjeta de crédito" }
                });

            migrationBuilder.InsertData(
                schema: "finance",
                table: "ReceiptSequences",
                columns: new[] { "id", "last_value" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_billing_plan_id_due_date",
                schema: "finance",
                table: "BillingPlanItems",
                columns: new[] { "billing_plan_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_billing_plan_id_financial_concept_id_insta~",
                schema: "finance",
                table: "BillingPlanItems",
                columns: new[] { "billing_plan_id", "financial_concept_id", "installment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_financial_concept_id",
                schema: "finance",
                table: "BillingPlanItems",
                column: "financial_concept_id");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_career_id_academic_year_name",
                schema: "finance",
                table: "BillingPlans",
                columns: new[] { "career_id", "academic_year", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_billing_plan_id",
                schema: "finance",
                table: "DebtGenerationBatches",
                column: "billing_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_idempotency_key",
                schema: "finance",
                table: "DebtGenerationBatches",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_public_id",
                schema: "finance",
                table: "DebtGenerationBatches",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialBenefits_career_id_is_active",
                schema: "finance",
                table: "FinancialBenefits",
                columns: new[] { "career_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialBenefits_code",
                schema: "finance",
                table: "FinancialBenefits",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialConcepts_code",
                schema: "finance",
                table: "FinancialConcepts",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRates_career_id_academic_year",
                schema: "finance",
                table: "FinancialRates",
                columns: new[] { "career_id", "academic_year" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRates_financial_concept_id_career_id_academic_year~",
                schema: "finance",
                table: "FinancialRates",
                columns: new[] { "financial_concept_id", "career_id", "academic_year", "student_condition" },
                unique: true,
                filter: "student_condition IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FinancialRates_Default",
                schema: "finance",
                table: "FinancialRates",
                columns: new[] { "financial_concept_id", "career_id", "academic_year" },
                unique: true,
                filter: "student_condition IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_payment_id_student_debt_id",
                schema: "finance",
                table: "PaymentAllocations",
                columns: new[] { "payment_id", "student_debt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_student_debt_id",
                schema: "finance",
                table: "PaymentAllocations",
                column: "student_debt_id");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_code",
                schema: "finance",
                table: "PaymentMethods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_kind",
                schema: "finance",
                table: "PaymentMethods",
                column: "kind",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReconciliations_payment_id",
                schema: "finance",
                table: "PaymentReconciliations",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReversals_payment_id",
                schema: "finance",
                table: "PaymentReversals",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReversals_public_id",
                schema: "finance",
                table: "PaymentReversals",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_confirmation_idempotency_key",
                schema: "finance",
                table: "Payments",
                column: "confirmation_idempotency_key",
                unique: true,
                filter: "confirmation_idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_payment_method_id",
                schema: "finance",
                table: "Payments",
                column: "payment_method_id");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_public_id",
                schema: "finance",
                table: "Payments",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_status_created_at",
                schema: "finance",
                table: "Payments",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_student_id_created_at",
                schema: "finance",
                table: "Payments",
                columns: new[] { "student_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_payment_id",
                schema: "finance",
                table: "Receipts",
                column: "payment_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_public_id",
                schema: "finance",
                table: "Receipts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_receipt_number",
                schema: "finance",
                table: "Receipts",
                column: "receipt_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_sequence_number",
                schema: "finance",
                table: "Receipts",
                column: "sequence_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_status_created_at",
                schema: "finance",
                table: "Receipts",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_applied_benefit_id",
                schema: "finance",
                table: "StudentDebts",
                column: "applied_benefit_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_billing_plan_item_id",
                schema: "finance",
                table: "StudentDebts",
                column: "billing_plan_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_debt_generation_batch_id_student_career_id_bil~",
                schema: "finance",
                table: "StudentDebts",
                columns: new[] { "debt_generation_batch_id", "student_career_id", "billing_plan_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_financial_concept_id",
                schema: "finance",
                table: "StudentDebts",
                column: "financial_concept_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_financial_rate_id",
                schema: "finance",
                table: "StudentDebts",
                column: "financial_rate_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_public_id",
                schema: "finance",
                table: "StudentDebts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_student_career_id_billing_plan_item_id",
                schema: "finance",
                table: "StudentDebts",
                columns: new[] { "student_career_id", "billing_plan_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_student_id_status_due_date",
                schema: "finance",
                table: "StudentDebts",
                columns: new[] { "student_id", "status", "due_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentAllocations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "PaymentReconciliations",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "PaymentReversals",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "Receipts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ReceiptSequences",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "StudentDebts",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "BillingPlanItems",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DebtGenerationBatches",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "FinancialBenefits",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "FinancialRates",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "BillingPlans",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "FinancialConcepts",
                schema: "finance");
        }
    }
}
