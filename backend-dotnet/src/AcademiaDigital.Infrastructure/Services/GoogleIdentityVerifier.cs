using AcademiaDigital.Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AcademiaDigital.Infrastructure.Services;

/// <summary>
/// Valida el ID token de Google Identity Services con las claves públicas de Google. La audiencia
/// debe ser el Client ID de esta aplicación (Authentication:Google:ClientId).
/// </summary>
public sealed class GoogleIdentityVerifier(IConfiguration configuration, ILogger<GoogleIdentityVerifier> logger)
    : IGoogleIdentityVerifier
{
    public string ClientId => configuration["Authentication:Google:ClientId"]?.Trim() ?? string.Empty;

    public async Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(idToken)) return null;
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [ClientId] });
            return new GoogleIdentity(payload.Email, payload.EmailVerified, payload.Name);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning("google_token_invalid {Reason}", ex.Message);
            return null;
        }
    }
}
