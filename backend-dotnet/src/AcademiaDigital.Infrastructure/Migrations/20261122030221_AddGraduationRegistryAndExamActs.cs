using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGraduationRegistryAndExamActs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "graduated_on",
                table: "Students",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registry_book",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "registry_folio",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "act_book",
                table: "ExamTables",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "act_folio",
                table: "ExamTables",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "graduated_on",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "registry_book",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "registry_folio",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "act_book",
                table: "ExamTables");

            migrationBuilder.DropColumn(
                name: "act_folio",
                table: "ExamTables");
        }
    }
}
