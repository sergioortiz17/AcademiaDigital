using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceConceptsPlansAndDebts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    career_id = table.Column<int>(type: "int", nullable: false),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingPlans_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingPlans_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialBenefits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    kind = table.Column<int>(type: "int", nullable: false),
                    scholarship_id = table.Column<int>(type: "int", nullable: true),
                    career_id = table.Column<int>(type: "int", nullable: true),
                    student_condition = table.Column<int>(type: "int", nullable: true),
                    percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialBenefits", x => x.Id);
                    table.CheckConstraint("CK_FinancialBenefits_Percentage", "[percentage] > 0 AND [percentage] <= 100");
                    table.CheckConstraint("CK_FinancialBenefits_Scholarship", "([kind] = 0 AND [scholarship_id] IS NULL) OR ([kind] = 1 AND [scholarship_id] IS NOT NULL)");
                    table.CheckConstraint("CK_FinancialBenefits_Validity", "[valid_from] IS NULL OR [valid_to] IS NULL OR [valid_to] >= [valid_from]");
                    table.ForeignKey(
                        name: "FK_FinancialBenefits_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialBenefits_Scholarships_scholarship_id",
                        column: x => x.scholarship_id,
                        principalTable: "Scholarships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialConcepts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialConcepts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DebtGenerationBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idempotency_key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    billing_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    generated_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    generated_debt_count = table.Column<int>(type: "int", nullable: false),
                    generated_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebtGenerationBatches", x => x.Id);
                    table.CheckConstraint("CK_DebtGenerationBatches_Count", "[generated_debt_count] >= 0");
                    table.CheckConstraint("CK_DebtGenerationBatches_Total", "[generated_total] >= 0");
                    table.ForeignKey(
                        name: "FK_DebtGenerationBatches_BillingPlans_billing_plan_id",
                        column: x => x.billing_plan_id,
                        principalTable: "BillingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DebtGenerationBatches_Users_generated_by_user_id",
                        column: x => x.generated_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingPlanItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    billing_plan_id = table.Column<long>(type: "bigint", nullable: false),
                    financial_concept_id = table.Column<int>(type: "int", nullable: false),
                    installment_number = table.Column<int>(type: "int", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPlanItems", x => x.Id);
                    table.CheckConstraint("CK_BillingPlanItems_Installment", "[installment_number] > 0");
                    table.ForeignKey(
                        name: "FK_BillingPlanItems_BillingPlans_billing_plan_id",
                        column: x => x.billing_plan_id,
                        principalTable: "BillingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingPlanItems_FinancialConcepts_financial_concept_id",
                        column: x => x.financial_concept_id,
                        principalTable: "FinancialConcepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialRates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    financial_concept_id = table.Column<int>(type: "int", nullable: false),
                    career_id = table.Column<int>(type: "int", nullable: false),
                    academic_year = table.Column<int>(type: "int", nullable: false),
                    student_condition = table.Column<int>(type: "int", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    surcharge_percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialRates", x => x.Id);
                    table.CheckConstraint("CK_FinancialRates_Amount", "[amount] > 0");
                    table.CheckConstraint("CK_FinancialRates_Surcharge", "[surcharge_percentage] >= 0 AND [surcharge_percentage] <= 100");
                    table.ForeignKey(
                        name: "FK_FinancialRates_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialRates_FinancialConcepts_financial_concept_id",
                        column: x => x.financial_concept_id,
                        principalTable: "FinancialConcepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentDebts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    debt_generation_batch_id = table.Column<long>(type: "bigint", nullable: false),
                    billing_plan_item_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: false),
                    financial_concept_id = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    base_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    surcharge_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    financial_rate_id = table.Column<long>(type: "bigint", nullable: false),
                    applied_benefit_id = table.Column<long>(type: "bigint", nullable: true),
                    calculation_snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDebts", x => x.Id);
                    table.CheckConstraint("CK_StudentDebts_Amounts", "[base_amount] > 0 AND [surcharge_amount] >= 0 AND [discount_amount] >= 0 AND [total_amount] >= 0 AND [paid_amount] >= 0 AND [paid_amount] <= [total_amount]");
                    table.CheckConstraint("CK_StudentDebts_Currency", "[currency] = 'ARS'");
                    table.ForeignKey(
                        name: "FK_StudentDebts_BillingPlanItems_billing_plan_item_id",
                        column: x => x.billing_plan_item_id,
                        principalTable: "BillingPlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_DebtGenerationBatches_debt_generation_batch_id",
                        column: x => x.debt_generation_batch_id,
                        principalTable: "DebtGenerationBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_FinancialBenefits_applied_benefit_id",
                        column: x => x.applied_benefit_id,
                        principalTable: "FinancialBenefits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_FinancialConcepts_financial_concept_id",
                        column: x => x.financial_concept_id,
                        principalTable: "FinancialConcepts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_FinancialRates_financial_rate_id",
                        column: x => x.financial_rate_id,
                        principalTable: "FinancialRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_StudentCareers_student_career_id",
                        column: x => x.student_career_id,
                        principalTable: "StudentCareers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDebts_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_billing_plan_id_due_date",
                table: "BillingPlanItems",
                columns: new[] { "billing_plan_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_billing_plan_id_financial_concept_id_installment_number",
                table: "BillingPlanItems",
                columns: new[] { "billing_plan_id", "financial_concept_id", "installment_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_financial_concept_id",
                table: "BillingPlanItems",
                column: "financial_concept_id");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_career_id_academic_year_name",
                table: "BillingPlans",
                columns: new[] { "career_id", "academic_year", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlans_created_by_user_id",
                table: "BillingPlans",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_billing_plan_id",
                table: "DebtGenerationBatches",
                column: "billing_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_generated_by_user_id",
                table: "DebtGenerationBatches",
                column: "generated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_idempotency_key",
                table: "DebtGenerationBatches",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebtGenerationBatches_public_id",
                table: "DebtGenerationBatches",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialBenefits_career_id_is_active",
                table: "FinancialBenefits",
                columns: new[] { "career_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialBenefits_code",
                table: "FinancialBenefits",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialBenefits_scholarship_id",
                table: "FinancialBenefits",
                column: "scholarship_id");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialConcepts_code",
                table: "FinancialConcepts",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRates_career_id",
                table: "FinancialRates",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRates_financial_concept_id_career_id_academic_year_student_condition",
                table: "FinancialRates",
                columns: new[] { "financial_concept_id", "career_id", "academic_year", "student_condition" },
                unique: true,
                filter: "[student_condition] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FinancialRates_Default",
                table: "FinancialRates",
                columns: new[] { "financial_concept_id", "career_id", "academic_year" },
                unique: true,
                filter: "[student_condition] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_applied_benefit_id",
                table: "StudentDebts",
                column: "applied_benefit_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_billing_plan_item_id",
                table: "StudentDebts",
                column: "billing_plan_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_debt_generation_batch_id_student_career_id_billing_plan_item_id",
                table: "StudentDebts",
                columns: new[] { "debt_generation_batch_id", "student_career_id", "billing_plan_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_financial_concept_id",
                table: "StudentDebts",
                column: "financial_concept_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_financial_rate_id",
                table: "StudentDebts",
                column: "financial_rate_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_public_id",
                table: "StudentDebts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_student_career_id_billing_plan_item_id",
                table: "StudentDebts",
                columns: new[] { "student_career_id", "billing_plan_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_student_id_status_due_date",
                table: "StudentDebts",
                columns: new[] { "student_id", "status", "due_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentDebts");

            migrationBuilder.DropTable(
                name: "BillingPlanItems");

            migrationBuilder.DropTable(
                name: "DebtGenerationBatches");

            migrationBuilder.DropTable(
                name: "FinancialBenefits");

            migrationBuilder.DropTable(
                name: "FinancialRates");

            migrationBuilder.DropTable(
                name: "BillingPlans");

            migrationBuilder.DropTable(
                name: "FinancialConcepts");
        }
    }
}
