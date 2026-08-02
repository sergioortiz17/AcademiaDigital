using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Module3StudentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line",
                table: "Students",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "Students",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "Students",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_relationship",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "Students",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Students",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateTable(
                name: "Commissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    YearNumber = table.Column<int>(type: "int", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Commissions_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    OptionsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CareerId = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentRequirements_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Scholarships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scholarships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentStatusHistory_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentStatusHistory_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentAcademicAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    StudyPlanId = table.Column<int>(type: "int", nullable: false),
                    CommissionId = table.Column<int>(type: "int", nullable: true),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    YearNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AssignedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAcademicAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAcademicAssignments_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAcademicAssignments_Commissions_CommissionId",
                        column: x => x.CommissionId,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAcademicAssignments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAcademicAssignments_StudyPlans_StudyPlanId",
                        column: x => x.StudyPlanId,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAcademicAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCustomFieldValues_CustomFieldDefinitions_CustomFieldDefinitionId",
                        column: x => x.CustomFieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentCustomFieldValues_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentCustomFieldValues_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentDocuments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentRequirementId = table.Column<int>(type: "int", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Observation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_DocumentRequirements_DocumentRequirementId",
                        column: x => x.DocumentRequirementId,
                        principalTable: "DocumentRequirements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentDocuments_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentScholarships",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    ScholarshipId = table.Column<int>(type: "int", nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentScholarships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentScholarships_Scholarships_ScholarshipId",
                        column: x => x.ScholarshipId,
                        principalTable: "Scholarships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentScholarships_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentScholarships_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Seed one auditable baseline event per existing student. Enum values
            // remain numerically compatible with the previous model.
            migrationBuilder.Sql(
                """
                INSERT INTO [StudentStatusHistory]
                    ([StudentId], [PreviousStatus], [NewStatus], [Reason], [ChangedAt], [ChangedByUserId])
                SELECT [id], [status], [status], N'Initial status imported by Module 3 migration',
                       SYSUTCDATETIME(), [user_id]
                FROM [Students];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_CareerId_AcademicYear_Code",
                table: "Commissions",
                columns: new[] { "CareerId", "AcademicYear", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_Key",
                table: "CustomFieldDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_CareerId",
                table: "DocumentRequirements",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_Code",
                table: "DocumentRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scholarships_Code",
                table: "Scholarships",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_AssignedByUserId",
                table: "StudentAcademicAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_CareerId",
                table: "StudentAcademicAssignments",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_CommissionId",
                table: "StudentAcademicAssignments",
                column: "CommissionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudentId",
                table: "StudentAcademicAssignments",
                column: "StudentId",
                unique: true,
                filter: "[IsCurrent] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudentId_AcademicYear",
                table: "StudentAcademicAssignments",
                columns: new[] { "StudentId", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudyPlanId",
                table: "StudentAcademicAssignments",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCustomFieldValues_CustomFieldDefinitionId",
                table: "StudentCustomFieldValues",
                column: "CustomFieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCustomFieldValues_StudentId_CustomFieldDefinitionId",
                table: "StudentCustomFieldValues",
                columns: new[] { "StudentId", "CustomFieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentCustomFieldValues_UpdatedByUserId",
                table: "StudentCustomFieldValues",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_DocumentRequirementId",
                table: "StudentDocuments",
                column: "DocumentRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_ReviewedByUserId",
                table: "StudentDocuments",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDocuments_StudentId_DocumentRequirementId_SubmittedAt",
                table: "StudentDocuments",
                columns: new[] { "StudentId", "DocumentRequirementId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentScholarships_ScholarshipId",
                table: "StudentScholarships",
                column: "ScholarshipId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentScholarships_StudentId_ScholarshipId_AcademicYear",
                table: "StudentScholarships",
                columns: new[] { "StudentId", "ScholarshipId", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentScholarships_UpdatedByUserId",
                table: "StudentScholarships",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStatusHistory_ChangedByUserId",
                table: "StudentStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStatusHistory_StudentId_ChangedAt",
                table: "StudentStatusHistory",
                columns: new[] { "StudentId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentAcademicAssignments");

            migrationBuilder.DropTable(
                name: "StudentCustomFieldValues");

            migrationBuilder.DropTable(
                name: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "StudentScholarships");

            migrationBuilder.DropTable(
                name: "StudentStatusHistory");

            migrationBuilder.DropTable(
                name: "Commissions");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions");

            migrationBuilder.DropTable(
                name: "DocumentRequirements");

            migrationBuilder.DropTable(
                name: "Scholarships");

            migrationBuilder.DropColumn(
                name: "address_line",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "city",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "emergency_contact_name",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "emergency_contact_phone",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "emergency_contact_relationship",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "province",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Students");
        }
    }
}
