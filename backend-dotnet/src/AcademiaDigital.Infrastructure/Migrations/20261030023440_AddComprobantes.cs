using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AcademiaDigital.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Comprobantes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    punto_venta = table.Column<int>(type: "integer", nullable: false),
                    tipo_comprobante = table.Column<int>(type: "integer", nullable: false),
                    concepto = table.Column<int>(type: "integer", nullable: false),
                    numero_comprobante = table.Column<long>(type: "bigint", nullable: true),
                    condicion_iva_receptor_id = table.Column<int>(type: "integer", nullable: false),
                    documento_receptor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    fecha_emision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_servicio_desde = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_servicio_hasta = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_vencimiento_pago = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    percepcion = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cae = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    cae_fecha_vencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    mensaje_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    solicitado_por_user_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    autorizado_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comprobantes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Comprobantes_Users_solicitado_por_user_id",
                        column: x => x.solicitado_por_user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_punto_venta_tipo_comprobante_estado",
                table: "Comprobantes",
                columns: new[] { "punto_venta", "tipo_comprobante", "estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_punto_venta_tipo_comprobante_numero_comprobante",
                table: "Comprobantes",
                columns: new[] { "punto_venta", "tipo_comprobante", "numero_comprobante" },
                unique: true,
                filter: "numero_comprobante IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Comprobantes_solicitado_por_user_id",
                table: "Comprobantes",
                column: "solicitado_por_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comprobantes");
        }
    }
}
