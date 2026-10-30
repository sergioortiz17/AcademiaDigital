namespace AcademiaDigital.Infrastructure.Services.Arca;

/// <summary>
/// Configuración del emisor para ARCA (ex AFIP). Se enlaza desde la sección "Arca" de appsettings.
/// </summary>
public class ArcaSettings
{
    /// <summary>
    /// Habilita el módulo. Cuando es false no se registra el monitor de certificado y las operaciones
    /// contra ARCA fallan con un mensaje claro (útil en desarrollo sin certificado).
    /// </summary>
    public bool Enabled { get; set; }

    public long Cuit { get; set; }
    public int PuntoVenta { get; set; }
    public string CertificadoPath { get; set; } = string.Empty;
    public string ClavePath { get; set; } = string.Empty;
    public bool ModoProduccion { get; set; }
    /// <summary>Condición IVA del emisor. Valores: "RI", "Monotributista", "Exento".</summary>
    public string CondicionIvaEmisor { get; set; } = "RI";
}
