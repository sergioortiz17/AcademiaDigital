namespace AcademiaDigital.Application.Interfaces;

public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>
/// Envío de correo. Implementaciones en Infrastructure: Amazon SES (producción) y un emisor que
/// solo escribe en el log (desarrollo). Devuelve false si no se pudo enviar; el detalle del error
/// lo registra la implementación. Nunca lanza por un fallo de envío.
/// </summary>
public interface IEmailSender
{
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);
}
