using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateIssuanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRequests_Users_user_id",
                table: "CertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRequests_user_id",
                table: "CertificateRequests");

            migrationBuilder.AddColumn<long>(
                name: "exam_registration_id",
                table: "CertificateRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "CertificateRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "CertificateRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "CertificateRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "reviewed_by_user_id",
                table: "CertificateRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "student_career_id",
                table: "CertificateRequests",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE cr
                SET student_career_id = selected_career.Id,
                    kind = CASE
                        WHEN LOWER(cr.certificate_type) LIKE '%alumno regular%' THEN 0
                        WHEN LOWER(cr.certificate_type) LIKE '%matrícula%'
                          OR LOWER(cr.certificate_type) LIKE '%matricula%'
                          OR LOWER(cr.certificate_type) LIKE '%inscripción%'
                          OR LOWER(cr.certificate_type) LIKE '%inscripcion%' THEN 1
                        WHEN LOWER(cr.certificate_type) LIKE '%materias aprobadas%' THEN 2
                        WHEN LOWER(cr.certificate_type) LIKE '%promedio%'
                          OR LOWER(cr.certificate_type) LIKE '%situación académica%'
                          OR LOWER(cr.certificate_type) LIKE '%situacion academica%' THEN 3
                        WHEN LOWER(cr.certificate_type) LIKE '%analítico%'
                          OR LOWER(cr.certificate_type) LIKE '%analitico%' THEN 4
                        WHEN LOWER(cr.certificate_type) LIKE '%egreso%'
                          OR LOWER(cr.certificate_type) LIKE '%estado académico%'
                          OR LOWER(cr.certificate_type) LIKE '%estado academico%' THEN 5
                        WHEN LOWER(cr.certificate_type) LIKE '%examen%' THEN 6
                        ELSE 3
                    END
                FROM CertificateRequests cr
                OUTER APPLY (
                    SELECT TOP (1) sc.Id
                    FROM Students s
                    INNER JOIN StudentCareers sc ON sc.StudentId = s.id
                    WHERE s.user_id = cr.user_id
                    ORDER BY CASE WHEN sc.IsActive = 1 THEN 0 ELSE 1 END,
                             CASE WHEN sc.CareerId = s.career_id THEN 0 ELSE 1 END,
                             sc.Id
                ) selected_career;

                WITH duplicates AS (
                    SELECT id,
                           ROW_NUMBER() OVER (
                               PARTITION BY user_id, student_career_id, kind, exam_registration_id
                               ORDER BY created_at DESC, id DESC) AS duplicate_order
                    FROM CertificateRequests
                    WHERE status IN (0, 1, 3)
                )
                UPDATE cr
                SET status = 2,
                    updated_at = SYSUTCDATETIME(),
                    rejection_reason = 'Superseded duplicate during M8 migration.'
                FROM CertificateRequests cr
                INNER JOIN duplicates d ON d.id = cr.id
                WHERE d.duplicate_order > 1;
                """);

            migrationBuilder.CreateTable(
                name: "CertificateIssuances",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    public_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    certificate_request_id = table.Column<long>(type: "bigint", nullable: false),
                    sequence_number = table.Column<long>(type: "bigint", nullable: false),
                    certificate_number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    snapshot_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    content_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    issued_by_user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateIssuances", x => x.id);
                    table.CheckConstraint("CK_CertificateIssuances_Sequence", "[sequence_number] > 0");
                    table.ForeignKey(
                        name: "FK_CertificateIssuances_CertificateRequests_certificate_request_id",
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

            migrationBuilder.CreateTable(
                name: "CertificateSequences",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateSequences", x => x.id);
                    table.CheckConstraint("CK_CertificateSequences_Singleton", "[id] = 1 AND [last_value] >= 0");
                });

            migrationBuilder.InsertData(
                table: "CertificateSequences",
                columns: new[] { "id", "last_value" },
                values: new object[] { 1, 0L });

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
                name: "IX_CertificateRequests_user_id_student_career_id_kind_exam_registration_id",
                table: "CertificateRequests",
                columns: new[] { "user_id", "student_career_id", "kind", "exam_registration_id" },
                unique: true,
                filter: "[status] IN (0, 1, 3)");

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

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRequests_ExamRegistrations_exam_registration_id",
                table: "CertificateRequests",
                column: "exam_registration_id",
                principalTable: "ExamRegistrations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRequests_StudentCareers_student_career_id",
                table: "CertificateRequests",
                column: "student_career_id",
                principalTable: "StudentCareers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRequests_Users_reviewed_by_user_id",
                table: "CertificateRequests",
                column: "reviewed_by_user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRequests_Users_user_id",
                table: "CertificateRequests",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRequests_ExamRegistrations_exam_registration_id",
                table: "CertificateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRequests_StudentCareers_student_career_id",
                table: "CertificateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRequests_Users_reviewed_by_user_id",
                table: "CertificateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_CertificateRequests_Users_user_id",
                table: "CertificateRequests");

            migrationBuilder.DropTable(
                name: "CertificateIssuances");

            migrationBuilder.DropTable(
                name: "CertificateSequences");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRequests_exam_registration_id",
                table: "CertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRequests_reviewed_by_user_id",
                table: "CertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRequests_student_career_id_status_created_at",
                table: "CertificateRequests");

            migrationBuilder.DropIndex(
                name: "IX_CertificateRequests_user_id_student_career_id_kind_exam_registration_id",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "exam_registration_id",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "reviewed_by_user_id",
                table: "CertificateRequests");

            migrationBuilder.DropColumn(
                name: "student_career_id",
                table: "CertificateRequests");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateRequests_user_id",
                table: "CertificateRequests",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateRequests_Users_user_id",
                table: "CertificateRequests",
                column: "user_id",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
