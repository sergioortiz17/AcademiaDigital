using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AcademiaDigital.Finance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDelinquencyAndExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicStudentSnapshots",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    StudentCareerId = table.Column<long>(type: "bigint", nullable: false),
                    student_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dni = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    legajo_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CareerId = table.Column<int>(type: "integer", nullable: false),
                    career_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CommissionId = table.Column<int>(type: "integer", nullable: true),
                    commission_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AcademicYear = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    synced_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicStudentSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DelinquencyAlertRules",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MinimumOutstandingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumDaysOverdue = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DelinquencyAlertRules", x => x.Id);
                    table.CheckConstraint("CK_DelinquencyAlertRules_Amount", "\"MinimumOutstandingAmount\" >= 0");
                    table.CheckConstraint("CK_DelinquencyAlertRules_Days", "\"MinimumDaysOverdue\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseEntries",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    ReversalOfExpenseId = table.Column<long>(type: "bigint", nullable: true),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    PurchaseNote = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttachmentStorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AttachmentContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AttachmentSha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseEntries", x => x.Id);
                    table.CheckConstraint("CK_ExpenseEntries_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_ExpenseEntries_Currency", "\"Currency\" = 'ARS'");
                    table.CheckConstraint("CK_ExpenseEntries_Reversal", "(\"Kind\" = 0 AND \"ReversalOfExpenseId\" IS NULL) OR (\"Kind\" = 1 AND \"ReversalOfExpenseId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ExpenseEntries_ExpenseCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "finance",
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseEntries_ExpenseEntries_ReversalOfExpenseId",
                        column: x => x.ReversalOfExpenseId,
                        principalSchema: "finance",
                        principalTable: "ExpenseEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentSnapshots_IsActive_CareerId_CommissionId_Aca~",
                schema: "finance",
                table: "AcademicStudentSnapshots",
                columns: new[] { "IsActive", "CareerId", "CommissionId", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicStudentSnapshots_StudentCareerId",
                schema: "finance",
                table: "AcademicStudentSnapshots",
                column: "StudentCareerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DelinquencyAlertRules_IsActive_MinimumDaysOverdue",
                schema: "finance",
                table: "DelinquencyAlertRules",
                columns: new[] { "IsActive", "MinimumDaysOverdue" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_code",
                schema: "finance",
                table: "ExpenseCategories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseEntries_CategoryId",
                schema: "finance",
                table: "ExpenseEntries",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseEntries_ExpenseDate_CategoryId",
                schema: "finance",
                table: "ExpenseEntries",
                columns: new[] { "ExpenseDate", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseEntries_PublicId",
                schema: "finance",
                table: "ExpenseEntries",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseEntries_ReversalOfExpenseId",
                schema: "finance",
                table: "ExpenseEntries",
                column: "ReversalOfExpenseId",
                unique: true,
                filter: "\"ReversalOfExpenseId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseEntries_SupplierName",
                schema: "finance",
                table: "ExpenseEntries",
                column: "SupplierName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicStudentSnapshots",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "DelinquencyAlertRules",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ExpenseEntries",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ExpenseCategories",
                schema: "finance");
        }
    }
}
