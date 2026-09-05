using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Careers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    total_credits = table.Column<int>(type: "integer", nullable: false),
                    duration_years = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Careers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CertificateSequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateSequences", x => x.id);
                    table.CheckConstraint("CK_CertificateSequences_Singleton", "id = 1 AND last_value >= 0");
                });

            migrationBuilder.CreateTable(
                name: "CooperativeEntities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    cuit = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contact_person = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CooperativeEntities", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CourseTypes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTypes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DataType = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    OptionsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialConcepts",
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
                name: "OutboxMessages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aggregate_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    available_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processing_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
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
                name: "Scholarships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scholarships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    last_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    dni = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: true),
                    cuil = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    phone_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    date_joined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    failed_login_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Commissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CareerId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AcademicYear = table.Column<int>(type: "integer", nullable: false),
                    YearNumber = table.Column<int>(type: "integer", nullable: false),
                    Shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                name: "Courses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.id);
                    table.ForeignKey(
                        name: "FK_Courses_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CareerId = table.Column<int>(type: "integer", nullable: true),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                name: "StudyPlans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlans", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudyPlans_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialRates",
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
                name: "FinancialBenefits",
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
                name: "ActiveSessions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveSessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_ActiveSessions_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Administratives",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Administratives", x => x.id);
                    table.ForeignKey(
                        name: "FK_Administratives_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingPlans",
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
                name: "Communications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    author_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.id);
                    table.ForeignKey(
                        name: "FK_Communications_Users_author_id",
                        column: x => x.author_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    legajo_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    address_line = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    province = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    emergency_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emergency_contact_relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    career_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.id);
                    table.ForeignKey(
                        name: "FK_Students_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Students_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    specialization_area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    address_line = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    province = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    emergency_contact_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    emergency_contact_relationship = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    emergency_contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.id);
                    table.ForeignKey(
                        name: "FK_Teachers_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionForms",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    commission_id = table.Column<int>(type: "integer", nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    terms_text = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    reservation_hours = table.Column<int>(type: "integer", nullable: false, defaultValue: 72),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionForms", x => x.id);
                    table.CheckConstraint("CK_AdmissionForms_Capacity", "capacity IS NULL OR (capacity >= 1 AND capacity <= 100000)");
                    table.ForeignKey(
                        name: "FK_AdmissionForms_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionForms_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamTables",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    call_number = table.Column<int>(type: "integer", nullable: false),
                    exam_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    registration_deadline_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    grading_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    grading_started_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTables", x => x.id);
                    table.CheckConstraint("CK_ExamTables_CallNumber", "call_number >= 1 AND call_number <= 10");
                    table.CheckConstraint("CK_ExamTables_Deadline", "registration_deadline_utc <= exam_date_utc");
                    table.ForeignKey(
                        name: "FK_ExamTables_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamTables_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamTables_Users_grading_started_by_user_id",
                        column: x => x.grading_started_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamTables_Users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherContests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    open_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    close_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: true),
                    career_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherContests", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherContests_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeacherContests_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CoursePrerequisites",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    study_plan_id = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    prerequisite_course_id = table.Column<int>(type: "integer", nullable: false),
                    prerequisite_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    minimum_required_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePrerequisites", x => x.id);
                    table.CheckConstraint("CK_CoursePrerequisites_NoSelfReference", "course_id <> prerequisite_course_id");
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_Courses_prerequisite_course_id",
                        column: x => x.prerequisite_course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoursePrerequisites_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentPeriods",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    study_plan_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    quotas_morning = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    quotas_afternoon = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    quotas_evening = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentPeriods", x => x.id);
                    table.ForeignKey(
                        name: "FK_EnrollmentPeriods_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnrollmentPeriods_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudyPlanCourses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    study_plan_id = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    year_number = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    course_type_id = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    credits = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    workload_hours = table.Column<int>(type: "integer", nullable: true),
                    is_annual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyPlanCourses", x => x.id);
                    table.CheckConstraint("CK_StudyPlanCourses_Semester", "semester IN (1, 2)");
                    table.CheckConstraint("CK_StudyPlanCourses_YearNumber", "year_number > 0");
                    table.ForeignKey(
                        name: "FK_StudyPlanCourses_CourseTypes_course_type_id",
                        column: x => x.course_type_id,
                        principalTable: "CourseTypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudyPlanCourses_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyPlanCourses_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillingPlanItems",
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
                name: "DebtGenerationBatches",
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
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confirmation_idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
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
                name: "StudentCareers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    CareerId = table.Column<int>(type: "integer", nullable: false),
                    EnrollmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCareers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCareers_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentCareers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    CustomFieldDefinitionId = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentCustomFieldValues_CustomFieldDefinitions_CustomField~",
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentRequirementId = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Observation = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
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
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    ScholarshipId = table.Column<int>(type: "integer", nullable: false),
                    AcademicYear = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "StudentStatusHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                name: "TeacherDocuments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    observation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherDocuments", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherDocuments_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherDocuments_Users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeachingPositions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    position_type = table.Column<int>(type: "integer", nullable: false),
                    max_students = table.Column<int>(type: "integer", nullable: false),
                    is_vacant = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deactivated_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    deactivation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    commission_id = table.Column<int>(type: "integer", nullable: true),
                    teacher_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingPositions", x => x.id);
                    table.CheckConstraint("CK_TeachingPositions_AssignmentState", "(is_vacant AND teacher_id IS NULL) OR (NOT is_vacant AND teacher_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TeachingPositions_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeachingPositions_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeachingPositions_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TeachingPositions_Users_deactivated_by_user_id",
                        column: x => x.deactivated_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admission_form_id = table.Column<int>(type: "integer", nullable: false),
                    applicant_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    applicant_dni = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submitted_fields_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    terms_accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reservation_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
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
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_form_id = table.Column<int>(type: "integer", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ExamTableReopenings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exam_table_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reopened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reopened_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTableReopenings", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamTableReopenings_ExamTables_exam_table_id",
                        column: x => x.exam_table_id,
                        principalTable: "ExamTables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamTableReopenings_Users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamTribunalMembers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exam_table_id = table.Column<long>(type: "bigint", nullable: false),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamTribunalMembers", x => x.id);
                    table.ForeignKey(
                        name: "FK_ExamTribunalMembers_ExamTables_exam_table_id",
                        column: x => x.exam_table_id,
                        principalTable: "ExamTables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamTribunalMembers_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContestApplications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    application_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    contest_id = table.Column<int>(type: "integer", nullable: false),
                    applicant_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestApplications", x => x.id);
                    table.ForeignKey(
                        name: "FK_ContestApplications_TeacherContests_contest_id",
                        column: x => x.contest_id,
                        principalTable: "TeacherContests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestApplications_Users_applicant_id",
                        column: x => x.applicant_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseApprovalRules",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    study_plan_course_id = table.Column<int>(type: "integer", nullable: false),
                    minimum_regular_grade = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    minimum_promotion_grade = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    minimum_final_exam_grade = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 6m),
                    minimum_attendance_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    requires_final_exam = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allows_promotion = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    policy_json = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseApprovalRules", x => x.id);
                    table.CheckConstraint("CK_CourseApprovalRules_FinalExamGrade", "minimum_final_exam_grade >= 1 AND minimum_final_exam_grade <= 10");
                    table.ForeignKey(
                        name: "FK_CourseApprovalRules_StudyPlanCourses_study_plan_course_id",
                        column: x => x.study_plan_course_id,
                        principalTable: "StudyPlanCourses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReconciliations",
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

            migrationBuilder.CreateTable(
                name: "Receipts",
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
                name: "StudentAcademicAssignments",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<long>(type: "bigint", nullable: false),
                    StudentCareerId = table.Column<long>(type: "bigint", nullable: false),
                    CareerId = table.Column<int>(type: "integer", nullable: false),
                    StudyPlanId = table.Column<int>(type: "integer", nullable: false),
                    CommissionId = table.Column<int>(type: "integer", nullable: true),
                    AcademicYear = table.Column<int>(type: "integer", nullable: false),
                    YearNumber = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCurrent = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                        name: "FK_StudentAcademicAssignments_StudentCareers_StudentCareerId",
                        column: x => x.StudentCareerId,
                        principalTable: "StudentCareers",
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
                name: "StudentDebts",
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

            migrationBuilder.CreateTable(
                name: "StudentRematriculations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: false),
                    career_id = table.Column<int>(type: "integer", nullable: false),
                    study_plan_id = table.Column<int>(type: "integer", nullable: false),
                    commission_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    year_number = table.Column<int>(type: "integer", nullable: false),
                    rematriculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRematriculations", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Careers_career_id",
                        column: x => x.career_id,
                        principalTable: "Careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_StudentCareers_student_career_id",
                        column: x => x.student_career_id,
                        principalTable: "StudentCareers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentRematriculations_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentStudyPlans",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: false),
                    study_plan_id = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    migration_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentStudyPlans", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentStudyPlans_StudentCareers_student_career_id",
                        column: x => x.student_career_id,
                        principalTable: "StudentCareers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentStudyPlans_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentStudyPlans_StudyPlans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "StudyPlans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSessions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    teaching_position_id = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    commission_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    session_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    units = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    edit_deadline_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_administratively_reopened = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessions", x => x.id);
                    table.CheckConstraint("CK_AttendanceSessions_TimeRange", "(scope = 0 AND start_time IS NOT NULL AND end_time IS NOT NULL AND end_time > start_time) OR (scope = 1 AND start_time IS NULL AND end_time IS NULL AND units = 1)");
                    table.CheckConstraint("CK_AttendanceSessions_Units", "units >= 1 AND units <= 12");
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessions_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    enrollment_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    final_grade = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    study_plan_course_id = table.Column<int>(type: "integer", nullable: true),
                    teaching_position_id = table.Column<int>(type: "integer", nullable: true),
                    enrollment_period_id = table.Column<int>(type: "integer", nullable: true),
                    shift = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_EnrollmentPeriods_enrollment_period_id",
                        column: x => x.enrollment_period_id,
                        principalTable: "EnrollmentPeriods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Enrollments_StudentCareers_student_career_id",
                        column: x => x.student_career_id,
                        principalTable: "StudentCareers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_StudyPlanCourses_study_plan_course_id",
                        column: x => x.study_plan_course_id,
                        principalTable: "StudyPlanCourses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Gradebooks",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    teaching_position_id = table.Column<int>(type: "integer", nullable: false),
                    course_id = table.Column<int>(type: "integer", nullable: false),
                    commission_id = table.Column<int>(type: "integer", nullable: false),
                    academic_year = table.Column<int>(type: "integer", nullable: false),
                    semester = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    submitted_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approved_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    published_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by_user_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gradebooks", x => x.id);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Commissions_commission_id",
                        column: x => x.commission_id,
                        principalTable: "Commissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Courses_course_id",
                        column: x => x.course_id,
                        principalTable: "Courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_approved_by_user_id",
                        column: x => x.approved_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_closed_by_user_id",
                        column: x => x.closed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_published_by_user_id",
                        column: x => x.published_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Gradebooks_Users_submitted_by_user_id",
                        column: x => x.submitted_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    teaching_position_id = table.Column<int>(type: "integer", nullable: false),
                    teacher_id = table.Column<long>(type: "bigint", nullable: false),
                    started_on = table.Column<DateOnly>(type: "date", nullable: false),
                    ended_on = table.Column<DateOnly>(type: "date", nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    assignment_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    end_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    assigned_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    ended_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAssignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Teachers_teacher_id",
                        column: x => x.teacher_id,
                        principalTable: "Teachers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_TeachingPositions_teaching_position_id",
                        column: x => x.teaching_position_id,
                        principalTable: "TeachingPositions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Users_assigned_by_user_id",
                        column: x => x.assigned_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Users_ended_by_user_id",
                        column: x => x.ended_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionAgreements",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_application_id = table.Column<long>(type: "bigint", nullable: false),
                    agreement_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionAgreements", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionAgreements_AdmissionApplications_admission_applica~",
                        column: x => x.admission_application_id,
                        principalTable: "AdmissionApplications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationDocuments",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_application_id = table.Column<long>(type: "bigint", nullable: false),
                    document_requirement_id = table.Column<int>(type: "integer", nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    observation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationDocuments", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationDocuments_AdmissionApplications_admissi~",
                        column: x => x.admission_application_id,
                        principalTable: "AdmissionApplications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationDocuments_DocumentRequirements_document~",
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

            migrationBuilder.CreateTable(
                name: "AdmissionApplicationStatusHistory",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    admission_application_id = table.Column<long>(type: "bigint", nullable: false),
                    from_status = table.Column<int>(type: "integer", nullable: true),
                    to_status = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionApplicationStatusHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_AdmissionApplicationStatusHistory_AdmissionApplications_adm~",
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

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
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
                name: "AttendanceSessionReopenings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attendance_session_id = table.Column<long>(type: "bigint", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reopened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reopened_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessionReopenings", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionReopenings_AttendanceSessions_attendance_s~",
                        column: x => x.attendance_session_id,
                        principalTable: "AttendanceSessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionReopenings_Users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attendance_session_id = table.Column<long>(type: "bigint", nullable: false),
                    enrollment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_AttendanceSessions_attendance_session_id",
                        column: x => x.attendance_session_id,
                        principalTable: "AttendanceSessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "Enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Users_updated_by_user_id",
                        column: x => x.updated_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamRegistrations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exam_table_id = table.Column<long>(type: "bigint", nullable: false),
                    enrollment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    attempt_number = table.Column<int>(type: "integer", nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    registered_by_user_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_enrollment_status = table.Column<int>(type: "integer", nullable: true),
                    previous_final_grade = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    result_applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRegistrations", x => x.id);
                    table.CheckConstraint("CK_ExamRegistrations_Attempt", "attempt_number >= 1");
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "Enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_ExamTables_exam_table_id",
                        column: x => x.exam_table_id,
                        principalTable: "ExamTables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Users_registered_by_user_id",
                        column: x => x.registered_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradebookEvaluations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gradebook_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    weight_percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    maximum_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookEvaluations", x => x.id);
                    table.CheckConstraint("CK_GradebookEvaluations_Maximum", "maximum_score > 0 AND maximum_score <= 100");
                    table.CheckConstraint("CK_GradebookEvaluations_Weight", "weight_percentage > 0 AND weight_percentage <= 100");
                    table.ForeignKey(
                        name: "FK_GradebookEvaluations_Gradebooks_gradebook_id",
                        column: x => x.gradebook_id,
                        principalTable: "Gradebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradebookReopenings",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gradebook_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reopened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reopened_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradebookReopenings", x => x.id);
                    table.ForeignKey(
                        name: "FK_GradebookReopenings_Gradebooks_gradebook_id",
                        column: x => x.gradebook_id,
                        principalTable: "Gradebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradebookReopenings_Users_reopened_by_user_id",
                        column: x => x.reopened_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceJustifications",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    attendance_record_id = table.Column<long>(type: "bigint", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    evidence_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceJustifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_AttendanceJustifications_AttendanceRecords_attendance_recor~",
                        column: x => x.attendance_record_id,
                        principalTable: "AttendanceRecords",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceJustifications_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateRequests",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    certificate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    student_career_id = table.Column<long>(type: "bigint", nullable: true),
                    exam_registration_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateRequests", x => x.id);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_ExamRegistrations_exam_registration_id",
                        column: x => x.exam_registration_id,
                        principalTable: "ExamRegistrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_StudentCareers_student_career_id",
                        column: x => x.student_career_id,
                        principalTable: "StudentCareers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_Users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificateRequests_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExamGradeRevisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exam_registration_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    grade = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamGradeRevisions", x => x.id);
                    table.CheckConstraint("CK_ExamGradeRevisions_Grade", "grade IS NULL OR (grade >= 0 AND grade <= 10)");
                    table.ForeignKey(
                        name: "FK_ExamGradeRevisions_ExamRegistrations_exam_registration_id",
                        column: x => x.exam_registration_id,
                        principalTable: "ExamRegistrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamGradeRevisions_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeEntryRevisions",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    gradebook_id = table.Column<long>(type: "bigint", nullable: false),
                    evaluation_id = table.Column<long>(type: "bigint", nullable: false),
                    enrollment_id = table.Column<long>(type: "bigint", nullable: false),
                    student_id = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeEntryRevisions", x => x.id);
                    table.CheckConstraint("CK_GradeEntryRevisions_Score", "score >= 0 AND score <= 100");
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Enrollments_enrollment_id",
                        column: x => x.enrollment_id,
                        principalTable: "Enrollments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_GradebookEvaluations_evaluation_id",
                        column: x => x.evaluation_id,
                        principalTable: "GradebookEvaluations",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Gradebooks_gradebook_id",
                        column: x => x.gradebook_id,
                        principalTable: "Gradebooks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Students_student_id",
                        column: x => x.student_id,
                        principalTable: "Students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradeEntryRevisions_Users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CertificateIssuances",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false),
                    certificate_request_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    certificate_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    snapshot_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    issued_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateIssuances", x => x.id);
                    table.CheckConstraint("CK_CertificateIssuances_Sequence", "sequence_number > 0");
                    table.ForeignKey(
                        name: "FK_CertificateIssuances_CertificateRequests_certificate_reques~",
                        column: x => x.certificate_request_id,
                        principalTable: "CertificateRequests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CertificateIssuances_Users_issued_by_user_id",
                        column: x => x.issued_by_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "CertificateSequences",
                columns: new[] { "id", "last_value" },
                values: new object[] { 1, 0L });

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

            migrationBuilder.InsertData(
                table: "ReceiptSequences",
                columns: new[] { "id", "last_value" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_ActiveSessions_user_id",
                table: "ActiveSessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Administratives_employee_number",
                table: "Administratives",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Administratives_user_id",
                table: "Administratives",
                column: "user_id");

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
                name: "IX_AdmissionApplicationDocuments_admission_application_id_docu~",
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
                name: "IX_AdmissionApplications_admission_form_id_status_created_at",
                table: "AdmissionApplications",
                columns: new[] { "admission_form_id", "status", "created_at" });

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
                name: "IX_AdmissionApplicationStatusHistory_admission_application_id_~",
                table: "AdmissionApplicationStatusHistory",
                columns: new[] { "admission_application_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplicationStatusHistory_changed_by_user_id",
                table: "AdmissionApplicationStatusHistory",
                column: "changed_by_user_id");

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
                name: "IX_AdmissionForms_commission_id",
                table: "AdmissionForms",
                column: "commission_id",
                unique: true,
                filter: "commission_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionForms_slug",
                table: "AdmissionForms",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceJustifications_attendance_record_id",
                table: "AttendanceJustifications",
                column: "attendance_record_id",
                unique: true,
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceJustifications_created_by_user_id",
                table: "AttendanceJustifications",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_attendance_session_id_enrollment_id",
                table: "AttendanceRecords",
                columns: new[] { "attendance_session_id", "enrollment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_enrollment_id",
                table: "AttendanceRecords",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_student_id_attendance_session_id",
                table: "AttendanceRecords",
                columns: new[] { "student_id", "attendance_session_id" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_updated_by_user_id",
                table: "AttendanceRecords",
                column: "updated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionReopenings_attendance_session_id_reopened_~",
                table: "AttendanceSessionReopenings",
                columns: new[] { "attendance_session_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionReopenings_reopened_by_user_id",
                table: "AttendanceSessionReopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_closed_by_user_id",
                table: "AttendanceSessions",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_commission_id",
                table: "AttendanceSessions",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_course_id_commission_id_academic_year_se~",
                table: "AttendanceSessions",
                columns: new[] { "course_id", "commission_id", "academic_year", "semester", "session_date", "start_time", "scope" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_created_by_user_id",
                table: "AttendanceSessions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_idempotency_key",
                table: "AttendanceSessions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessions_teaching_position_id",
                table: "AttendanceSessions",
                column: "teaching_position_id");

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_billing_plan_id_due_date",
                table: "BillingPlanItems",
                columns: new[] { "billing_plan_id", "due_date" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPlanItems_billing_plan_id_financial_concept_id_insta~",
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
                name: "IX_Careers_code",
                table: "Careers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateIssuances_certificate_number",
                table: "CertificateIssuances",
                column: "certificate_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateIssuances_certificate_request_id",
                table: "CertificateIssuances",
                column: "certificate_request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateIssuances_issued_by_user_id",
                table: "CertificateIssuances",
                column: "issued_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateIssuances_public_id",
                table: "CertificateIssuances",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateIssuances_sequence_number",
                table: "CertificateIssuances",
                column: "sequence_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_exam_registration_id",
                table: "CertificateRequests",
                column: "exam_registration_id");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_reviewed_by_user_id",
                table: "CertificateRequests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_student_career_id_status_created_at",
                table: "CertificateRequests",
                columns: new[] { "student_career_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_user_id_student_career_id_kind_exam_reg~",
                table: "CertificateRequests",
                columns: new[] { "user_id", "student_career_id", "kind", "exam_registration_id" },
                unique: true,
                filter: "status IN (0, 1, 3)");

            migrationBuilder.CreateIndex(
                name: "IX_Commissions_CareerId_AcademicYear_Code",
                table: "Commissions",
                columns: new[] { "CareerId", "AcademicYear", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Communications_author_id",
                table: "Communications",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContestApplications_applicant_id",
                table: "ContestApplications",
                column: "applicant_id");

            migrationBuilder.CreateIndex(
                name: "IX_ContestApplications_contest_id_applicant_id",
                table: "ContestApplications",
                columns: new[] { "contest_id", "applicant_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CooperativeEntities_cuit",
                table: "CooperativeEntities",
                column: "cuit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseApprovalRules_study_plan_course_id",
                table: "CourseApprovalRules",
                column: "study_plan_course_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_course_id",
                table: "CoursePrerequisites",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_prerequisite_course_id",
                table: "CoursePrerequisites",
                column: "prerequisite_course_id");

            migrationBuilder.CreateIndex(
                name: "IX_CoursePrerequisites_study_plan_id_course_id_prerequisite_co~",
                table: "CoursePrerequisites",
                columns: new[] { "study_plan_id", "course_id", "prerequisite_course_id" },
                unique: true,
                filter: "is_active = true");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_career_id_code",
                table: "Courses",
                columns: new[] { "career_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseTypes_code",
                table: "CourseTypes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_Key",
                table: "CustomFieldDefinitions",
                column: "Key",
                unique: true);

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
                name: "IX_DocumentRequirements_CareerId",
                table: "DocumentRequirements",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRequirements_Code",
                table: "DocumentRequirements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPeriods_career_id",
                table: "EnrollmentPeriods",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentPeriods_study_plan_id",
                table: "EnrollmentPeriods",
                column: "study_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_course_id",
                table: "Enrollments",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_enrollment_period_id",
                table: "Enrollments",
                column: "enrollment_period_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_career_id",
                table: "Enrollments",
                column: "student_career_id");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_id_course_id_academic_year_semester",
                table: "Enrollments",
                columns: new[] { "student_id", "course_id", "academic_year", "semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_student_id_status",
                table: "Enrollments",
                columns: new[] { "student_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_study_plan_course_id_status",
                table: "Enrollments",
                columns: new[] { "study_plan_course_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_teaching_position_id",
                table: "Enrollments",
                column: "teaching_position_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGradeRevisions_created_by_user_id",
                table: "ExamGradeRevisions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGradeRevisions_exam_registration_id",
                table: "ExamGradeRevisions",
                column: "exam_registration_id",
                unique: true,
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_ExamGradeRevisions_exam_registration_id_version",
                table: "ExamGradeRevisions",
                columns: new[] { "exam_registration_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_enrollment_id_attempt_number",
                table: "ExamRegistrations",
                columns: new[] { "enrollment_id", "attempt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_exam_table_id_enrollment_id",
                table: "ExamRegistrations",
                columns: new[] { "exam_table_id", "enrollment_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_registered_by_user_id",
                table: "ExamRegistrations",
                column: "registered_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_student_id",
                table: "ExamRegistrations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTableReopenings_exam_table_id_reopened_at",
                table: "ExamTableReopenings",
                columns: new[] { "exam_table_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ExamTableReopenings_reopened_by_user_id",
                table: "ExamTableReopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_course_id_exam_date_utc_call_number",
                table: "ExamTables",
                columns: new[] { "course_id", "exam_date_utc", "call_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_created_by_user_id",
                table: "ExamTables",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_grading_started_by_user_id",
                table: "ExamTables",
                column: "grading_started_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_idempotency_key",
                table: "ExamTables",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamTables_published_by_user_id",
                table: "ExamTables",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTribunalMembers_exam_table_id_role",
                table: "ExamTribunalMembers",
                columns: new[] { "exam_table_id", "role" },
                unique: true,
                filter: "role = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ExamTribunalMembers_exam_table_id_teacher_id",
                table: "ExamTribunalMembers",
                columns: new[] { "exam_table_id", "teacher_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamTribunalMembers_teacher_id",
                table: "ExamTribunalMembers",
                column: "teacher_id");

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
                name: "IX_FinancialRates_financial_concept_id_career_id_academic_year~",
                table: "FinancialRates",
                columns: new[] { "financial_concept_id", "career_id", "academic_year", "student_condition" },
                unique: true,
                filter: "student_condition IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_FinancialRates_Default",
                table: "FinancialRates",
                columns: new[] { "financial_concept_id", "career_id", "academic_year" },
                unique: true,
                filter: "student_condition IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GradebookEvaluations_gradebook_id_display_order",
                table: "GradebookEvaluations",
                columns: new[] { "gradebook_id", "display_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradebookEvaluations_gradebook_id_name",
                table: "GradebookEvaluations",
                columns: new[] { "gradebook_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradebookReopenings_gradebook_id_reopened_at",
                table: "GradebookReopenings",
                columns: new[] { "gradebook_id", "reopened_at" });

            migrationBuilder.CreateIndex(
                name: "IX_GradebookReopenings_reopened_by_user_id",
                table: "GradebookReopenings",
                column: "reopened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_approved_by_user_id",
                table: "Gradebooks",
                column: "approved_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_closed_by_user_id",
                table: "Gradebooks",
                column: "closed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_commission_id",
                table: "Gradebooks",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_course_id_commission_id_academic_year_semester",
                table: "Gradebooks",
                columns: new[] { "course_id", "commission_id", "academic_year", "semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_created_by_user_id",
                table: "Gradebooks",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_idempotency_key",
                table: "Gradebooks",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_published_by_user_id",
                table: "Gradebooks",
                column: "published_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_submitted_by_user_id",
                table: "Gradebooks",
                column: "submitted_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Gradebooks_teaching_position_id",
                table: "Gradebooks",
                column: "teaching_position_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_created_by_user_id",
                table: "GradeEntryRevisions",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_enrollment_id",
                table: "GradeEntryRevisions",
                column: "enrollment_id");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_evaluation_id_enrollment_id",
                table: "GradeEntryRevisions",
                columns: new[] { "evaluation_id", "enrollment_id" },
                unique: true,
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_evaluation_id_enrollment_id_version",
                table: "GradeEntryRevisions",
                columns: new[] { "evaluation_id", "enrollment_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_gradebook_id_student_id",
                table: "GradeEntryRevisions",
                columns: new[] { "gradebook_id", "student_id" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeEntryRevisions_student_id",
                table: "GradeEntryRevisions",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_deduplication_key",
                table: "OutboxMessages",
                column: "deduplication_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_status_available_at",
                table: "OutboxMessages",
                columns: new[] { "status", "available_at" });

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
                filter: "confirmation_idempotency_key IS NOT NULL");

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
                name: "IX_StudentAcademicAssignments_StudentCareerId",
                table: "StudentAcademicAssignments",
                column: "StudentCareerId",
                unique: true,
                filter: "\"IsCurrent\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudentId_AcademicYear",
                table: "StudentAcademicAssignments",
                columns: new[] { "StudentId", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAcademicAssignments_StudyPlanId",
                table: "StudentAcademicAssignments",
                column: "StudyPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentCareers_CareerId_IsActive",
                table: "StudentCareers",
                columns: new[] { "CareerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentCareers_StudentId_CareerId",
                table: "StudentCareers",
                columns: new[] { "StudentId", "CareerId" },
                unique: true);

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
                name: "IX_StudentDebts_applied_benefit_id",
                table: "StudentDebts",
                column: "applied_benefit_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_billing_plan_item_id",
                table: "StudentDebts",
                column: "billing_plan_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDebts_debt_generation_batch_id_student_career_id_bil~",
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
                name: "IX_StudentRematriculations_career_id_academic_year",
                table: "StudentRematriculations",
                columns: new[] { "career_id", "academic_year" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_commission_id",
                table: "StudentRematriculations",
                column: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_created_by_user_id",
                table: "StudentRematriculations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_student_career_id_academic_year",
                table: "StudentRematriculations",
                columns: new[] { "student_career_id", "academic_year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_student_id",
                table: "StudentRematriculations",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRematriculations_study_plan_id",
                table: "StudentRematriculations",
                column: "study_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_career_id",
                table: "Students",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_Students_legajo_number",
                table: "Students",
                column: "legajo_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_user_id",
                table: "Students",
                column: "user_id",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_student_career_id",
                table: "StudentStudyPlans",
                column: "student_career_id",
                unique: true,
                filter: "is_current = true");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_student_id",
                table: "StudentStudyPlans",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentStudyPlans_study_plan_id",
                table: "StudentStudyPlans",
                column: "study_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_course_id",
                table: "StudyPlanCourses",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_course_type_id",
                table: "StudyPlanCourses",
                column: "course_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_study_plan_id_course_id",
                table: "StudyPlanCourses",
                columns: new[] { "study_plan_id", "course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlanCourses_study_plan_id_year_number_semester_sort_or~",
                table: "StudyPlanCourses",
                columns: new[] { "study_plan_id", "year_number", "semester", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyPlans_career_id_version_number",
                table: "StudyPlans",
                columns: new[] { "career_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_assigned_by_user_id",
                table: "TeacherAssignments",
                column: "assigned_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_ended_by_user_id",
                table: "TeacherAssignments",
                column: "ended_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_teacher_id_is_current",
                table: "TeacherAssignments",
                columns: new[] { "teacher_id", "is_current" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_teaching_position_id",
                table: "TeacherAssignments",
                column: "teaching_position_id",
                unique: true,
                filter: "is_current");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContests_career_id",
                table: "TeacherContests",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherContests_course_id",
                table: "TeacherContests",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_reviewed_by_user_id",
                table: "TeacherDocuments",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_teacher_id_document_type_version",
                table: "TeacherDocuments",
                columns: new[] { "teacher_id", "document_type", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherDocuments_teacher_id_submitted_at",
                table: "TeacherDocuments",
                columns: new[] { "teacher_id", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_employee_number",
                table: "Teachers",
                column: "employee_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_academic_year_semester_is_active",
                table: "TeachingPositions",
                columns: new[] { "academic_year", "semester", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_commission_id_course_id",
                table: "TeachingPositions",
                columns: new[] { "commission_id", "course_id" });

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_course_id",
                table: "TeachingPositions",
                column: "course_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_deactivated_by_user_id",
                table: "TeachingPositions",
                column: "deactivated_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingPositions_teacher_id",
                table: "TeachingPositions",
                column: "teacher_id");

            migrationBuilder.CreateIndex(
                name: "IX_Users_dni",
                table: "Users",
                column: "dni",
                unique: true,
                filter: "dni IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_email",
                table: "Users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicEvents");

            migrationBuilder.DropTable(
                name: "ActiveSessions");

            migrationBuilder.DropTable(
                name: "Administratives");

            migrationBuilder.DropTable(
                name: "AdmissionAgreements");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationDocuments");

            migrationBuilder.DropTable(
                name: "AdmissionApplicationStatusHistory");

            migrationBuilder.DropTable(
                name: "AdmissionFormFields");

            migrationBuilder.DropTable(
                name: "AttendanceJustifications");

            migrationBuilder.DropTable(
                name: "AttendanceSessionReopenings");

            migrationBuilder.DropTable(
                name: "CertificateIssuances");

            migrationBuilder.DropTable(
                name: "CertificateSequences");

            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropTable(
                name: "ContestApplications");

            migrationBuilder.DropTable(
                name: "CooperativeEntities");

            migrationBuilder.DropTable(
                name: "CourseApprovalRules");

            migrationBuilder.DropTable(
                name: "CoursePrerequisites");

            migrationBuilder.DropTable(
                name: "ExamGradeRevisions");

            migrationBuilder.DropTable(
                name: "ExamTableReopenings");

            migrationBuilder.DropTable(
                name: "ExamTribunalMembers");

            migrationBuilder.DropTable(
                name: "GradebookReopenings");

            migrationBuilder.DropTable(
                name: "GradeEntryRevisions");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PaymentAllocations");

            migrationBuilder.DropTable(
                name: "PaymentReconciliations");

            migrationBuilder.DropTable(
                name: "PaymentReversals");

            migrationBuilder.DropTable(
                name: "Receipts");

            migrationBuilder.DropTable(
                name: "ReceiptSequences");

            migrationBuilder.DropTable(
                name: "StudentAcademicAssignments");

            migrationBuilder.DropTable(
                name: "StudentCustomFieldValues");

            migrationBuilder.DropTable(
                name: "StudentDocuments");

            migrationBuilder.DropTable(
                name: "StudentRematriculations");

            migrationBuilder.DropTable(
                name: "StudentScholarships");

            migrationBuilder.DropTable(
                name: "StudentStatusHistory");

            migrationBuilder.DropTable(
                name: "StudentStudyPlans");

            migrationBuilder.DropTable(
                name: "TeacherAssignments");

            migrationBuilder.DropTable(
                name: "TeacherDocuments");

            migrationBuilder.DropTable(
                name: "AdmissionApplications");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "CertificateRequests");

            migrationBuilder.DropTable(
                name: "TeacherContests");

            migrationBuilder.DropTable(
                name: "GradebookEvaluations");

            migrationBuilder.DropTable(
                name: "StudentDebts");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions");

            migrationBuilder.DropTable(
                name: "DocumentRequirements");

            migrationBuilder.DropTable(
                name: "AdmissionForms");

            migrationBuilder.DropTable(
                name: "AttendanceSessions");

            migrationBuilder.DropTable(
                name: "ExamRegistrations");

            migrationBuilder.DropTable(
                name: "Gradebooks");

            migrationBuilder.DropTable(
                name: "BillingPlanItems");

            migrationBuilder.DropTable(
                name: "DebtGenerationBatches");

            migrationBuilder.DropTable(
                name: "FinancialBenefits");

            migrationBuilder.DropTable(
                name: "FinancialRates");

            migrationBuilder.DropTable(
                name: "PaymentMethods");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "ExamTables");

            migrationBuilder.DropTable(
                name: "BillingPlans");

            migrationBuilder.DropTable(
                name: "Scholarships");

            migrationBuilder.DropTable(
                name: "FinancialConcepts");

            migrationBuilder.DropTable(
                name: "EnrollmentPeriods");

            migrationBuilder.DropTable(
                name: "StudentCareers");

            migrationBuilder.DropTable(
                name: "StudyPlanCourses");

            migrationBuilder.DropTable(
                name: "TeachingPositions");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "CourseTypes");

            migrationBuilder.DropTable(
                name: "StudyPlans");

            migrationBuilder.DropTable(
                name: "Commissions");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropTable(
                name: "Careers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
