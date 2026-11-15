using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AcademiaDigital.Infrastructure.Services.Email;

/// <summary>
/// Envío por Amazon SES v2 con el rol de la instancia (sin claves). El remitente tiene que
/// pertenecer a una identidad verificada en SES (en producción, el dominio itsc.dryvn.org).
/// Con la cuenta en sandbox SES solo entrega a direcciones verificadas: el resto falla y queda
/// registrado como error, sin cortar el pedido del usuario.
/// </summary>
public sealed class SesEmailSender(IAmazonSimpleEmailServiceV2 ses, EmailOptions options, ILogger<SesEmailSender> logger)
    : IEmailSender
{
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            await ses.SendEmailAsync(new SendEmailRequest
            {
                FromEmailAddress = options.From,
                Destination = new Destination { ToAddresses = [message.To] },
                Content = new EmailContent
                {
                    Simple = new Message
                    {
                        Subject = new Content { Data = message.Subject, Charset = "UTF-8" },
                        Body = new Body
                        {
                            Html = new Content { Data = message.HtmlBody, Charset = "UTF-8" },
                            Text = new Content { Data = message.TextBody, Charset = "UTF-8" }
                        }
                    }
                }
            }, ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No se registra el destinatario ni el cuerpo (contiene el enlace con el token).
            logger.LogError(ex, "{Event} {ErrorType}", "email_send_failed", ex.GetType().Name);
            return false;
        }
    }
}
