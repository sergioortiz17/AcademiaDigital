using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    bool ValidateToken(string token);
    long? GetUserIdFromToken(string token);
    UserRole? GetUserRoleFromToken(string token);

    /// <summary>
    /// Token de recuperación de contraseña (15 minutos). Lleva la institución, para que no sirva
    /// en otra (los ids de usuario se repiten entre bases), y una huella de la contraseña
    /// actual, para que deje de servir apenas se usa.
    /// </summary>
    string GeneratePasswordResetToken(long userId, string tenant, string passwordStamp);

    /// <summary>Lee y valida firma, vencimiento y propósito. Null si el token no es válido.</summary>
    PasswordResetClaims? ReadPasswordResetToken(string token);
}

public sealed record PasswordResetClaims(long UserId, string Tenant, string PasswordStamp);
