using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class ComprobanteConfiguration : IEntityTypeConfiguration<Comprobante>
{
    public void Configure(EntityTypeBuilder<Comprobante> builder)
    {
        builder.ToTable("Comprobantes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(c => c.PuntoVenta).HasColumnName("punto_venta").IsRequired();
        builder.Property(c => c.TipoComprobante).HasColumnName("tipo_comprobante").IsRequired();
        builder.Property(c => c.Concepto).HasColumnName("concepto").IsRequired();
        builder.Property(c => c.NumeroComprobante).HasColumnName("numero_comprobante");
        builder.Property(c => c.CondicionIvaReceptorId).HasColumnName("condicion_iva_receptor_id").IsRequired();
        builder.Property(c => c.DocumentoReceptor).HasColumnName("documento_receptor").HasMaxLength(20);
        builder.Property(c => c.FechaEmision).HasColumnName("fecha_emision").IsRequired();
        builder.Property(c => c.FechaServicioDesde).HasColumnName("fecha_servicio_desde");
        builder.Property(c => c.FechaServicioHasta).HasColumnName("fecha_servicio_hasta");
        builder.Property(c => c.FechaVencimientoPago).HasColumnName("fecha_vencimiento_pago");
        builder.Property(c => c.Subtotal).HasColumnName("subtotal").HasColumnType("numeric(18,2)");
        builder.Property(c => c.Percepcion).HasColumnName("percepcion").HasColumnType("numeric(18,2)");
        builder.Property(c => c.Total).HasColumnName("total").HasColumnType("numeric(18,2)");
        builder.Property(c => c.Estado).HasColumnName("estado").HasConversion<int>().HasDefaultValue(ComprobanteEstado.Pendiente);
        builder.Property(c => c.Cae).HasColumnName("cae").HasMaxLength(20);
        builder.Property(c => c.CaeFechaVencimiento).HasColumnName("cae_fecha_vencimiento");
        builder.Property(c => c.MensajeError).HasColumnName("mensaje_error").HasMaxLength(2000);
        builder.Property(c => c.SolicitadoPorUserId).HasColumnName("solicitado_por_user_id").IsRequired();
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.AutorizadoAt).HasColumnName("autorizado_at");

        builder.HasIndex(c => new { c.PuntoVenta, c.TipoComprobante, c.Estado });
        builder.HasIndex(c => new { c.PuntoVenta, c.TipoComprobante, c.NumeroComprobante })
            .IsUnique()
            .HasFilter("numero_comprobante IS NOT NULL");

        builder.HasOne(c => c.SolicitadoPorUser)
               .WithMany()
               .HasForeignKey(c => c.SolicitadoPorUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
