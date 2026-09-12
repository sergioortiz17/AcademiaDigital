using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameCommissionToDivision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionForms_Commissions_commission_id",
                table: "AdmissionForms");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Commissions_commission_id",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Gradebooks_Commissions_commission_id",
                table: "Gradebooks");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentAcademicAssignments_Commissions_CommissionId",
                table: "StudentAcademicAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentRematriculations_Commissions_commission_id",
                table: "StudentRematriculations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingPositions_Commissions_commission_id",
                table: "TeachingPositions");

            // Renombrar la tabla (preserva datos, PK y la FK a Careers) en vez de drop+create.
            migrationBuilder.RenameTable(name: "Commissions", newName: "Divisions");
            migrationBuilder.Sql("ALTER TABLE \"Divisions\" RENAME CONSTRAINT \"PK_Commissions\" TO \"PK_Divisions\";");
            migrationBuilder.RenameIndex(
                name: "IX_Commissions_CareerId_AcademicYear_Code",
                table: "Divisions",
                newName: "IX_Divisions_CareerId_AcademicYear_Code");
            migrationBuilder.RenameIndex(
                name: "IX_Commissions_CareerId_AcademicYear_Name",
                table: "Divisions",
                newName: "IX_Divisions_CareerId_AcademicYear_Name");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionForms_commission_id",
                table: "AdmissionForms");

            migrationBuilder.RenameColumn(
                name: "commission_id",
                table: "TeachingPositions",
                newName: "division_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeachingPositions_commission_id_course_id",
                table: "TeachingPositions",
                newName: "IX_TeachingPositions_division_id_course_id");

            migrationBuilder.RenameColumn(
                name: "commission_id",
                table: "StudentRematriculations",
                newName: "division_id");

            migrationBuilder.RenameIndex(
                name: "IX_StudentRematriculations_commission_id",
                table: "StudentRematriculations",
                newName: "IX_StudentRematriculations_division_id");

            migrationBuilder.RenameColumn(
                name: "CommissionId",
                table: "StudentAcademicAssignments",
                newName: "DivisionId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentAcademicAssignments_CommissionId",
                table: "StudentAcademicAssignments",
                newName: "IX_StudentAcademicAssignments_DivisionId");

            migrationBuilder.RenameColumn(
                name: "commission_id",
                table: "Gradebooks",
                newName: "division_id");

            migrationBuilder.RenameIndex(
                name: "IX_Gradebooks_course_id_commission_id_academic_year_semester",
                table: "Gradebooks",
                newName: "IX_Gradebooks_course_id_division_id_academic_year_semester");

            migrationBuilder.RenameIndex(
                name: "IX_Gradebooks_commission_id",
                table: "Gradebooks",
                newName: "IX_Gradebooks_division_id");

            migrationBuilder.RenameColumn(
                name: "commission_id",
                table: "AttendanceSessions",
                newName: "division_id");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_course_id_commission_id_academic_year_se~",
                table: "AttendanceSessions",
                newName: "IX_AttendanceSessions_course_id_division_id_academic_year_seme~");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_commission_id",
                table: "AttendanceSessions",
                newName: "IX_AttendanceSessions_division_id");

            migrationBuilder.RenameColumn(
                name: "commission_id",
                table: "AdmissionForms",
                newName: "division_id");

            migrationBuilder.AddColumn<int>(
                name: "AdmissionYear",
                table: "StudentCareers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionForms_division_id",
                table: "AdmissionForms",
                column: "division_id",
                unique: true,
                filter: "division_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionForms_Divisions_division_id",
                table: "AdmissionForms",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Divisions_division_id",
                table: "AttendanceSessions",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Gradebooks_Divisions_division_id",
                table: "Gradebooks",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAcademicAssignments_Divisions_DivisionId",
                table: "StudentAcademicAssignments",
                column: "DivisionId",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentRematriculations_Divisions_division_id",
                table: "StudentRematriculations",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingPositions_Divisions_division_id",
                table: "TeachingPositions",
                column: "division_id",
                principalTable: "Divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdmissionForms_Divisions_division_id",
                table: "AdmissionForms");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceSessions_Divisions_division_id",
                table: "AttendanceSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Gradebooks_Divisions_division_id",
                table: "Gradebooks");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentAcademicAssignments_Divisions_DivisionId",
                table: "StudentAcademicAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentRematriculations_Divisions_division_id",
                table: "StudentRematriculations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingPositions_Divisions_division_id",
                table: "TeachingPositions");

            migrationBuilder.RenameTable(name: "Divisions", newName: "Commissions");
            migrationBuilder.Sql("ALTER TABLE \"Commissions\" RENAME CONSTRAINT \"PK_Divisions\" TO \"PK_Commissions\";");
            migrationBuilder.RenameIndex(
                name: "IX_Divisions_CareerId_AcademicYear_Code",
                table: "Commissions",
                newName: "IX_Commissions_CareerId_AcademicYear_Code");
            migrationBuilder.RenameIndex(
                name: "IX_Divisions_CareerId_AcademicYear_Name",
                table: "Commissions",
                newName: "IX_Commissions_CareerId_AcademicYear_Name");

            migrationBuilder.DropIndex(
                name: "IX_AdmissionForms_division_id",
                table: "AdmissionForms");

            migrationBuilder.DropColumn(
                name: "AdmissionYear",
                table: "StudentCareers");

            migrationBuilder.RenameColumn(
                name: "division_id",
                table: "TeachingPositions",
                newName: "commission_id");

            migrationBuilder.RenameIndex(
                name: "IX_TeachingPositions_division_id_course_id",
                table: "TeachingPositions",
                newName: "IX_TeachingPositions_commission_id_course_id");

            migrationBuilder.RenameColumn(
                name: "division_id",
                table: "StudentRematriculations",
                newName: "commission_id");

            migrationBuilder.RenameIndex(
                name: "IX_StudentRematriculations_division_id",
                table: "StudentRematriculations",
                newName: "IX_StudentRematriculations_commission_id");

            migrationBuilder.RenameColumn(
                name: "DivisionId",
                table: "StudentAcademicAssignments",
                newName: "CommissionId");

            migrationBuilder.RenameIndex(
                name: "IX_StudentAcademicAssignments_DivisionId",
                table: "StudentAcademicAssignments",
                newName: "IX_StudentAcademicAssignments_CommissionId");

            migrationBuilder.RenameColumn(
                name: "division_id",
                table: "Gradebooks",
                newName: "commission_id");

            migrationBuilder.RenameIndex(
                name: "IX_Gradebooks_division_id",
                table: "Gradebooks",
                newName: "IX_Gradebooks_commission_id");

            migrationBuilder.RenameIndex(
                name: "IX_Gradebooks_course_id_division_id_academic_year_semester",
                table: "Gradebooks",
                newName: "IX_Gradebooks_course_id_commission_id_academic_year_semester");

            migrationBuilder.RenameColumn(
                name: "division_id",
                table: "AttendanceSessions",
                newName: "commission_id");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_division_id",
                table: "AttendanceSessions",
                newName: "IX_AttendanceSessions_commission_id");

            migrationBuilder.RenameIndex(
                name: "IX_AttendanceSessions_course_id_division_id_academic_year_seme~",
                table: "AttendanceSessions",
                newName: "IX_AttendanceSessions_course_id_commission_id_academic_year_se~");

            migrationBuilder.RenameColumn(
                name: "division_id",
                table: "AdmissionForms",
                newName: "commission_id");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionForms_commission_id",
                table: "AdmissionForms",
                column: "commission_id",
                unique: true,
                filter: "commission_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AdmissionForms_Commissions_commission_id",
                table: "AdmissionForms",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceSessions_Commissions_commission_id",
                table: "AttendanceSessions",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Gradebooks_Commissions_commission_id",
                table: "Gradebooks",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentAcademicAssignments_Commissions_CommissionId",
                table: "StudentAcademicAssignments",
                column: "CommissionId",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentRematriculations_Commissions_commission_id",
                table: "StudentRematriculations",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingPositions_Commissions_commission_id",
                table: "TeachingPositions",
                column: "commission_id",
                principalTable: "Commissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
