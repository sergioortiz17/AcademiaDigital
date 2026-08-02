using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingUserProfileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columnas ya agregadas por la migración AddUserProfileFields en bases sanas;
            // se chequea existencia porque algunas bases quedaron con el historial de
            // migraciones inconsistente y no las tienen aplicadas.
            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'gender') IS NULL
                    ALTER TABLE [Users] ADD [gender] nvarchar(1) NULL;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'cuil') IS NULL
                    ALTER TABLE [Users] ADD [cuil] nvarchar(20) NULL;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'birth_date') IS NULL
                    ALTER TABLE [Users] ADD [birth_date] datetime2 NULL;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'phone_code') IS NULL
                    ALTER TABLE [Users] ADD [phone_code] nvarchar(10) NULL;
            ");

            migrationBuilder.Sql(@"
                IF COL_LENGTH('Users', 'phone') IS NULL
                    ALTER TABLE [Users] ADD [phone] nvarchar(20) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: estas columnas pertenecen a la migración AddUserProfileFields,
            // que es la responsable de crearlas y eliminarlas.
        }
    }
}
