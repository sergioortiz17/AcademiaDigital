using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;

namespace AcademiaDigital.Infrastructure.Services.Arca;

/// <summary>
/// Monitorea el certificado digital de ARCA y avisa por log cuando está por vencer o vencido.
/// Solo se registra cuando el módulo ARCA está habilitado (ver DependencyInjection).
/// </summary>
public class CertificadoMonitorService : BackgroundService
{
    private readonly ArcaSettings _settings;
    private readonly ILogger<CertificadoMonitorService> _logger;

    public CertificadoMonitorService(IOptions<ArcaSettings> options, ILogger<CertificadoMonitorService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChequearCertificado();

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            ChequearCertificado();
        }
    }

    private void ChequearCertificado()
    {
        var certPath = _settings.CertificadoPath;

        if (!Path.IsPathRooted(certPath))
            certPath = Path.Combine(AppContext.BaseDirectory, certPath);

        if (!File.Exists(certPath))
        {
            _logger.LogError("Certificado ARCA no encontrado en {Path}. No se podrán emitir comprobantes.", certPath);
            return;
        }

        try
        {
            var cert = new X509Certificate2(certPath);
            var diasRestantes = (cert.NotAfter - DateTime.Now).Days;

            if (diasRestantes <= 0)
                _logger.LogError(
                    "CERTIFICADO ARCA VENCIDO. Venció el {Fecha:dd/MM/yyyy}. No se pueden emitir comprobantes electrónicos.",
                    cert.NotAfter);
            else if (diasRestantes <= 15)
                _logger.LogError(
                    "CERTIFICADO ARCA vence en {Dias} días ({Fecha:dd/MM/yyyy}). Renovar de inmediato.",
                    diasRestantes, cert.NotAfter);
            else if (diasRestantes <= 30)
                _logger.LogWarning(
                    "Certificado ARCA vence en {Dias} días ({Fecha:dd/MM/yyyy}). Tramitar renovación pronto.",
                    diasRestantes, cert.NotAfter);
            else
                _logger.LogInformation(
                    "Certificado ARCA OK. Vence el {Fecha:dd/MM/yyyy} ({Dias} días restantes).",
                    cert.NotAfter, diasRestantes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al leer el certificado ARCA en {Path}.", certPath);
        }
    }
}
