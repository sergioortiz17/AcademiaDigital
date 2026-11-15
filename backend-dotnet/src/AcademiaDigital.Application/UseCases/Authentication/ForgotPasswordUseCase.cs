using System.Net;
using System.Security.Cryptography;
using System.Text;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;

namespace AcademiaDigital.Application.UseCases.Authentication;

/// <summary>
/// Pedido de recuperación de contraseña. Si el email (y el DNI, cuando se informa) corresponden
/// a un usuario activo, envía por correo un enlace con un token de un solo uso. La respuesta es
/// siempre la misma, exista o no la cuenta, para no revelar qué emails están registrados; por eso
/// el token nunca vuelve en la respuesta.
/// </summary>
public class ForgotPasswordUseCase(
    IUserRepository userRepository,
    ITokenService tokenService,
    IEmailSender emailSender,
    ITenantContext tenantContext)
{
    public const int TokenMinutes = 15;

    /// <param name="resetPageUrl">URL absoluta de la pantalla de reset del frontend de esta institución.</param>
    public async Task ExecuteAsync(string email, string? dni, string resetPageUrl, CancellationToken ct = default)
    {
        var user = await userRepository.FindByEmailAsync(email.Trim(), ct);
        if (user is null || !user.IsActive)
            return;

        // El DNI es opcional en el contrato, pero si llega tiene que coincidir.
        if (!string.IsNullOrWhiteSpace(dni) && !string.Equals(user.Dni?.Trim(), dni.Trim(), StringComparison.Ordinal))
            return;

        var tenant = tenantContext.Current;
        var token = tokenService.GeneratePasswordResetToken(user.Id, tenant.Slug, PasswordResetStamp.For(user.Password));
        var link = $"{resetPageUrl}?token={Uri.EscapeDataString(token)}";

        await emailSender.SendAsync(BuildMessage(user.Email, user.Username, tenant.Name, link), ct);
    }

    private static EmailMessage BuildMessage(string to, string name, string institution, string link)
    {
        var safeName = WebUtility.HtmlEncode(name);
        var safeInstitution = WebUtility.HtmlEncode(institution);
        var safeLink = WebUtility.HtmlEncode(link);

        var subject = $"Recuperación de contraseña — {institution}";
        var text =
            $"Hola {name}:\n\n" +
            $"Recibimos un pedido para restablecer tu contraseña en {institution}.\n" +
            $"Para elegir una nueva, abre este enlace (vence en {TokenMinutes} minutos y sirve una sola vez):\n\n" +
            $"{link}\n\n" +
            "Si no lo pediste, ignora este correo: tu contraseña no cambia.";
        var html =
            $"<p>Hola {safeName}:</p>" +
            $"<p>Recibimos un pedido para restablecer tu contraseña en <strong>{safeInstitution}</strong>.</p>" +
            $"<p><a href=\"{safeLink}\">Elegir una contraseña nueva</a></p>" +
            $"<p>El enlace vence en {TokenMinutes} minutos y sirve una sola vez.</p>" +
            "<p>Si no lo pediste, ignora este correo: tu contraseña no cambia.</p>";

        return new EmailMessage(to, subject, html, text);
    }
}

/// <summary>
/// Huella de la contraseña actual que viaja en el token de recuperación: al cambiar la
/// contraseña la huella cambia y el token deja de servir (un solo uso). No expone el hash.
/// </summary>
public static class PasswordResetStamp
{
    public static string For(string passwordHash)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..16];
}
