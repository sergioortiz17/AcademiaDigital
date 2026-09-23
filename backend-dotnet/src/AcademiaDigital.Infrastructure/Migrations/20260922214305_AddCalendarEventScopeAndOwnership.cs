using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCalendarEventScopeAndOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AcademicEvents",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPublished",
                table: "AcademicEvents",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "AcademicEvents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AcademicEvents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AcademicEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "CourseSectionId",
                table: "AcademicEvents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedByUserId",
                table: "AcademicEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "AcademicEvents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicEvents_CourseSectionId",
                table: "AcademicEvents",
                column: "CourseSectionId");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicEvents_CreatedByUserId",
                table: "AcademicEvents",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicEvents_CourseSections_CourseSectionId",
                table: "AcademicEvents",
                column: "CourseSectionId",
                principalTable: "CourseSections",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcademicEvents_Users_CreatedByUserId",
                table: "AcademicEvents",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcademicEvents_CourseSections_CourseSectionId",
                table: "AcademicEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_AcademicEvents_Users_CreatedByUserId",
                table: "AcademicEvents");

            migrationBuilder.DropIndex(
                name: "IX_AcademicEvents_CourseSectionId",
                table: "AcademicEvents");

            migrationBuilder.DropIndex(
                name: "IX_AcademicEvents_CreatedByUserId",
                table: "AcademicEvents");

            migrationBuilder.DropColumn(
                name: "CourseSectionId",
                table: "AcademicEvents");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "AcademicEvents");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "AcademicEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "AcademicEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPublished",
                table: "AcademicEvents",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "AcademicEvents",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "AcademicEvents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AcademicEvents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "NOW()");
        }
    }
}
