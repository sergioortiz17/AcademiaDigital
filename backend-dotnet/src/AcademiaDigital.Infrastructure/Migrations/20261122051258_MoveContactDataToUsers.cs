using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveContactDataToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line",
                table: "Users",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "alternate_phone",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "Users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "district",
                table: "Users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "document_type",
                table: "Users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "Users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_relationship",
                table: "Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "health_insurance",
                table: "Users",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "neighborhood",
                table: "Users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "province",
                table: "Users",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            // Copia lo que alumnos y docentes ya cargaron (la ficha de alumno tiene prioridad).
            migrationBuilder.Sql(@"
UPDATE ""Users"" u SET address_line = t.address_line, city = t.city, province = t.province, postal_code = t.postal_code, emergency_contact_name = t.emergency_contact_name, emergency_contact_relationship = t.emergency_contact_relationship, emergency_contact_phone = t.emergency_contact_phone, health_insurance = t.health_insurance
FROM ""Teachers"" t WHERE t.user_id = u.id;
UPDATE ""Users"" u SET address_line = COALESCE(s.address_line, u.address_line), city = COALESCE(s.city, u.city), province = COALESCE(s.province, u.province), postal_code = COALESCE(s.postal_code, u.postal_code), emergency_contact_name = COALESCE(s.emergency_contact_name, u.emergency_contact_name), emergency_contact_relationship = COALESCE(s.emergency_contact_relationship, u.emergency_contact_relationship), emergency_contact_phone = COALESCE(s.emergency_contact_phone, u.emergency_contact_phone), health_insurance = COALESCE(s.health_insurance, u.health_insurance)
FROM ""Students"" s WHERE s.user_id = u.id;");

            migrationBuilder.DropColumn(
                name: "address_line",
                table: "Teachers");


            migrationBuilder.DropColumn(
                name: "city",
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
                name: "health_insurance",
                table: "Teachers");


            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "Teachers");


            migrationBuilder.DropColumn(
                name: "province",
                table: "Teachers");


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
                name: "health_insurance",
                table: "Students");


            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "Students");


            migrationBuilder.DropColumn(
                name: "province",
                table: "Students");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_line",
                table: "Teachers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "Teachers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "Teachers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "Teachers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_relationship",
                table: "Teachers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "health_insurance",
                table: "Teachers",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "Teachers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "province",
                table: "Teachers",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "address_line",
                table: "Students",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_name",
                table: "Students",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_phone",
                table: "Students",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "emergency_contact_relationship",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "health_insurance",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                table: "Students",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);


            migrationBuilder.AddColumn<string>(
                name: "province",
                table: "Students",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ""Students"" s SET address_line = u.address_line, city = u.city, province = u.province, postal_code = u.postal_code, emergency_contact_name = u.emergency_contact_name, emergency_contact_relationship = u.emergency_contact_relationship, emergency_contact_phone = u.emergency_contact_phone, health_insurance = u.health_insurance FROM ""Users"" u WHERE s.user_id = u.id;
UPDATE ""Teachers"" t SET address_line = u.address_line, city = u.city, province = u.province, postal_code = u.postal_code, emergency_contact_name = u.emergency_contact_name, emergency_contact_relationship = u.emergency_contact_relationship, emergency_contact_phone = u.emergency_contact_phone, health_insurance = u.health_insurance FROM ""Users"" u WHERE t.user_id = u.id;");

            migrationBuilder.DropColumn(
                name: "address_line",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "alternate_phone",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "city",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "country",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "district",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "document_type",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "emergency_contact_name",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "emergency_contact_phone",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "emergency_contact_relationship",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "health_insurance",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "neighborhood",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "postal_code",
                table: "Users");


            migrationBuilder.DropColumn(
                name: "province",
                table: "Users");

        }
    }
}
