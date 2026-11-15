using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademiaDigital.Infrastructure.Services.Email;

/// <summary>
/// Emisor de desarrollo: no envía nada, escribe el correo en el log para poder seguir el
/// enlace de recuperación en local. Es el emisor por defecto (Email:Provider distinto de Ses);
/// en producción Terraform configura Ses.
/// </summary>
public sealed class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogWarning(
            "Correo NO enviado (Email:Provider=Log). Para: {To} | Asunto: {Subject}\n{Body}",
            message.To, message.Subject, message.TextBody);
        return Task.FromResult(true);
    }
}
