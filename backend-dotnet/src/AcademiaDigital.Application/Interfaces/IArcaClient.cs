namespace AcademiaDigital.Application.Interfaces;

/// <summary>
/// Puerto hacia ARCA (ex AFIP). Abstrae los web services SOAP de facturación electrónica (WSAA +
/// WSFEv1) y de consulta de padrón. La implementación vive en Infrastructure; Application y los
/// casos de uso solo conocen esta interfaz y sus DTOs de transporte.
/// </summary>
public interface IArcaClient
{
    /// <summary>Punto de venta configurado por defecto para el emisor.</summary>
    int PuntoVentaPorDefecto { get; }

    /// <summary>
    /// Solicita a ARCA el CAE para un nuevo comprobante del punto de venta y tipo indicados. ARCA
    /// determina el número correlativo. Nunca lanza por rechazos de negocio: devuelve un resultado
    /// con <see cref="ArcaAutorizacionResult.Autorizado"/> en false y el detalle en MensajeError.
    /// </summary>
    Task<ArcaAutorizacionResult> AutorizarComprobanteAsync(
        int puntoVenta, int tipoComprobante, ArcaAutorizacionRequest request, CancellationToken ct = default);

    /// <summary>Número del último comprobante autorizado por ARCA para el punto de venta y tipo.</summary>
    Task<long> ObtenerUltimoComprobanteAsync(int puntoVenta, int tipoComprobante, CancellationToken ct = default);

    /// <summary>Consulta el padrón de ARCA por DNI (7-8 dígitos) o CUIT/CUIL (11 dígitos).</summary>
    Task<ArcaPersonaResult?> ConsultarPersonaAsync(string documento, CancellationToken ct = default);

    /// <summary>Devuelve el XML crudo de condiciones IVA de receptor admitidas por ARCA.</summary>
    Task<string> ObtenerCondicionesIvaReceptorAsync(CancellationToken ct = default);
}

/// <summary>Datos necesarios para pedir la autorización (CAE) de un comprobante a ARCA.</summary>
public sealed class ArcaAutorizacionRequest
{
    /// <summary>1 = Productos, 2 = Servicios, 3 = Productos y Servicios.</summary>
    public int Concepto { get; set; }

    /// <summary>Código ARCA de condición IVA del receptor (1=RI, 4=Exento, 5=CF, 6=Monotributo, 7=No Resp, 13=Monotrib Social).</summary>
    public int CondicionIVAReceptorId { get; set; }

    /// <summary>CUIT, CUIL o DNI del receptor. El servicio determina el tipo de documento.</summary>
    public string? DocumentoReceptor { get; set; }

    public DateTime FechaEmision { get; set; }
    public DateTime? FechaServicioDesde { get; set; }
    public DateTime? FechaServicioHasta { get; set; }
    public DateTime FechaVencimientoPago { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Percepcion { get; set; }
    public decimal Total { get; set; }

    public ArcaComprobanteAsociado? ComprobanteAsociado { get; set; }
}

/// <summary>Comprobante original asociado (p. ej. para notas de crédito/débito).</summary>
public sealed class ArcaComprobanteAsociado
{
    /// <summary>Código ARCA del tipo de comprobante original.</summary>
    public int Tipo { get; set; }
    public int PtoVta { get; set; }
    public long Nro { get; set; }
    /// <summary>Fecha en formato yyyyMMdd.</summary>
    public string CbteFch { get; set; } = string.Empty;
}

/// <summary>Resultado de la autorización de un comprobante en ARCA.</summary>
public sealed class ArcaAutorizacionResult
{
    public bool Autorizado { get; set; }
    public string? CAE { get; set; }
    public DateOnly CAEFechaVencimiento { get; set; }
    public long NumeroComprobante { get; set; }
    public string? MensajeError { get; set; }
}

/// <summary>Datos de una persona devueltos por el padrón de ARCA.</summary>
public sealed class ArcaPersonaResult
{
    public long Cuit { get; set; }
    /// <summary>FISICA o JURIDICA.</summary>
    public string TipoPersona { get; set; } = string.Empty;
    public string Denominacion { get; set; } = string.Empty;
    /// <summary>ACTIVO, INACTIVO, etc.</summary>
    public string EstadoClave { get; set; } = string.Empty;
    public string? CondicionIva { get; set; }
    public string? Localidad { get; set; }
    public string? Provincia { get; set; }
    public string? CodigoPostal { get; set; }
}
