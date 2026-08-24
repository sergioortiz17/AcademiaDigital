using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherProfilesAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT [user_id]
                    FROM [Teachers]
                    GROUP BY [user_id]
                    HAVING COUNT(*) > 1)
                THROW 51000, 'Cannot enforce one teacher profile per user because duplicate teacher user links exist.', 1;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers");

            migrationBuilder.AddColumn<string>(
                name: "address_line",
                table: "Teachers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "Teachers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_at",
                table: "Teachers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "deactivated_by_user_id",
                table: "Teachers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deactivation_reason",
                table: "Teachers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "Teachers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "Teachers",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_relationship",
                table: "Teachers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "Teachers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "province",
                table: "Teachers",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "address_line",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "city",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "deactivated_at",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "deactivated_by_user_id",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "deactivation_reason",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "emergency_contact_name",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "emergency_contact_phone",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "emergency_contact_relationship",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "province",
                table: "Teachers");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_user_id",
                table: "Teachers",
                column: "user_id");
        }
    }
}
