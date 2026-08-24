using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissionCapacityAndWaitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "capacity",
                table: "AdmissionForms",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "reservation_expires_at",
                table: "AdmissionApplications",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AdmissionForms_Capacity",
                table: "AdmissionForms",
                sql: "[capacity] IS NULL OR ([capacity] >= 1 AND [capacity] <= 100000)");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionApplications_admission_form_id_status_created_at",
                table: "AdmissionApplications",
                columns: new[] { "admission_form_id", "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AdmissionForms_Capacity",
                table: "AdmissionForms");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionApplications_admission_form_id_status_created_at",
                table: "AdmissionApplications");

            migrationBuilder.DropColumn(
                name: "capacity",
                table: "AdmissionForms");

            migrationBuilder.AlterColumn<DateTime>(
                name: "reservation_expires_at",
                table: "AdmissionApplications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
