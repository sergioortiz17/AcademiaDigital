namespace AcademiaDigital.Application.Interfaces;

/// <summary>Identidad que Google confirmó para el usuario (cuenta con la que inició sesión).</summary>
public sealed record GoogleIdentity(string Email, bool EmailVerified, string? Name);

/// <summary>Verifica el token que entrega "Iniciar sesión con Google" (firma, emisor, audiencia y vencimiento).</summary>
public interface IGoogleIdentityVerifier
{
    /// <summary>Client ID configurado; vacío si el login con Google está desactivado.</summary>
    string ClientId { get; }

    /// <summary>Null si el token es inválido o no fue emitido para esta aplicación.</summary>
    Task<GoogleIdentity?> VerifyAsync(string idToken, CancellationToken ct = default);
}
