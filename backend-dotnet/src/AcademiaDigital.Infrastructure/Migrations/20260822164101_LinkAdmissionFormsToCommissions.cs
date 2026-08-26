using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkAdmissionFormsToCommissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "commission_id",
                table: "AdmissionForms",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionForms_commission_id",
                table: "AdmissionForms",
                column: "commission_id",
                unique: true,
                filter: "[commission_id] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionForms_Commissions_commission_id",
                table: "AdmissionForms",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionForms_Commissions_commission_id",
                table: "AdmissionForms");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionForms_commission_id",
                table: "AdmissionForms");

            migrationBuilder.DropColumn(
                name: "commission_id",
                table: "AdmissionForms");
        }
    }
}
