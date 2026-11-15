using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRegistrationApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "approved_by_user_id",
                table: "Users",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "registration_status",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_registration_status_date_joined",
                table: "Users",
                columns: new[] { "registration_status", "date_joined" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_registration_status_date_joined",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "registration_status",
                table: "Users");
        }
    }
}
