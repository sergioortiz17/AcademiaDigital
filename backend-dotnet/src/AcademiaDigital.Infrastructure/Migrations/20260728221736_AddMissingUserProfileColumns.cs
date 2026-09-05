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
            // No-op: columns already added by 20260525120000_AddUserProfileFields.
            // (La rama fix usaba raw SQL de SQL Server (COL_LENGTH/nvarchar/datetime2) que no
            // aplica sobre PostgreSQL. En la consolidacion v3 las migraciones se regeneran como
            // un unico InitialCreate de Postgres, por lo que este archivo desaparece; se deja
            // no-op para mantener la solucion compilable mientras tanto.)
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: see Up().
        }
    }
}
