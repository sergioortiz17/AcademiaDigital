using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AcademiaDigital.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : ITokenService
{
    private readonly string _secretKey = configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey not configured");
    private readonly int _expirationDays = int.Parse(configuration["Jwt:ExpirationDays"] ?? "7");

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("id", user.Id.ToString()),
            new Claim("role", ((int)user.Role).ToString()),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_expirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public long? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var idClaim = jwt.Claims.FirstOrDefault(c => c.Type == "id");

            return idClaim != null && long.TryParse(idClaim.Value, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }

    public UserRole? GetUserRoleFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "role");

            return roleClaim != null && int.TryParse(roleClaim.Value, out var role)
                ? (UserRole)role
                : null;
        }
        catch
        {
            return null;
        }
    }

    private const string ResetPurpose = "password-reset";

    public string GeneratePasswordResetToken(long userId, string tenant, string passwordStamp)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("id", userId.ToString()),
            new Claim("purpose", ResetPurpose),
            new Claim("tenant", tenant),   // no sirve en otra institución (ids repetidos)
            new Claim("pst", passwordStamp) // huella de la contraseña: un solo uso
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public PasswordResetClaims? ReadPasswordResetToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));

            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            var jwt = handler.ReadJwtToken(token);
            string? ClaimValue(string type) => jwt.Claims.FirstOrDefault(c => c.Type == type)?.Value;

            if (ClaimValue("purpose") != ResetPurpose) return null;
            if (!long.TryParse(ClaimValue("id"), out var id)) return null;
            var tenant = ClaimValue("tenant");
            var stamp = ClaimValue("pst");
            if (string.IsNullOrEmpty(tenant) || string.IsNullOrEmpty(stamp)) return null;

            return new PasswordResetClaims(id, tenant, stamp);
        }
        catch
        {
            return null;
        }
    }
}
