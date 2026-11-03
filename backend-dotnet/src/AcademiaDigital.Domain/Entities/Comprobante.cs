namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Comprobante electrónico emitido contra ARCA (ex AFIP). Persiste el pedido de autorización y su
/// resultado (CAE). El número de comprobante lo asigna ARCA al autorizar, por eso es nulo hasta
/// que el estado pasa a <see cref="ComprobanteEstado.Autorizado"/>.
/// </summary>
public class Comprobante
{
    public long Id { get; set; }

    /// <summary>Punto de venta habilitado en ARCA.</summary>
    public int PuntoVenta { get; set; }

    /// <summary>Código ARCA del tipo de comprobante (1=Factura A, 6=Factura B, 11=Factura C, etc.).</summary>
    public int TipoComprobante { get; set; }

    /// <summary>1 = Productos, 2 = Servicios, 3 = Productos y Servicios.</summary>
    public int Concepto { get; set; }

    /// <summary>Número asignado por ARCA al autorizar. Nulo mientras está pendiente.</summary>
    public long? NumeroComprobante { get; set; }

    /// <summary>Código ARCA de condición IVA del receptor (1=RI, 4=Exento, 5=CF, 6=Monotributo, etc.).</summary>
    public int CondicionIvaReceptorId { get; set; }

    /// <summary>CUIT, CUIL o DNI del receptor. ARCA determina el tipo de documento automáticamente.</summary>
    public string? DocumentoReceptor { get; set; }

    public DateTime FechaEmision { get; set; }
    public DateTime? FechaServicioDesde { get; set; }
    public DateTime? FechaServicioHasta { get; set; }
    public DateTime? FechaVencimientoPago { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Percepcion { get; set; }
    public decimal Total { get; set; }

    public ComprobanteEstado Estado { get; set; } = ComprobanteEstado.Pendiente;

    /// <summary>CAE devuelto por ARCA cuando el comprobante queda autorizado.</summary>
    public string? Cae { get; set; }
    public DateOnly? CaeFechaVencimiento { get; set; }

    /// <summary>Detalle del rechazo u observaciones de ARCA cuando no se autoriza.</summary>
    public string? MensajeError { get; set; }

    public long SolicitadoPorUserId { get; set; }
    public User SolicitadoPorUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AutorizadoAt { get; set; }

    /// <summary>Marca el comprobante como autorizado con los datos del CAE devuelto por ARCA.</summary>
    public void MarcarAutorizado(string cae, DateOnly caeVencimiento, long numeroComprobante, DateTime nowUtc)
    {
        if (Estado != ComprobanteEstado.Pendiente)
            throw new InvalidOperationException("Solo un comprobante pendiente puede autorizarse.");
        if (string.IsNullOrWhiteSpace(cae))
            throw new ArgumentException("El CAE es obligatorio para autorizar el comprobante.");

        Estado = ComprobanteEstado.Autorizado;
        Cae = cae;
        CaeFechaVencimiento = caeVencimiento;
        NumeroComprobante = numeroComprobante;
        MensajeError = null;
        AutorizadoAt = nowUtc;
    }

    /// <summary>Marca el comprobante como rechazado con el detalle devuelto por ARCA.</summary>
    public void MarcarRechazado(string? mensajeError)
    {
        if (Estado != ComprobanteEstado.Pendiente)
            throw new InvalidOperationException("Solo un comprobante pendiente puede rechazarse.");

        Estado = ComprobanteEstado.Rechazado;
        MensajeError = string.IsNullOrWhiteSpace(mensajeError)
            ? "ARCA rechazó el comprobante sin detalle."
            : mensajeError;
    }
}

public enum ComprobanteEstado
{
    Pendiente = 0,
    Autorizado = 1,
    Rechazado = 2
}
