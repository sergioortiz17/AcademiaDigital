using System.Threading.RateLimiting;
using AcademiaDigital.API.Middleware;
using Microsoft.AspNetCore.RateLimiting;

namespace AcademiaDigital.API.Security;

/// <summary>Enlace de la pantalla de reset y freno al abuso del pedido de recuperación.</summary>
public static class PasswordResetLinks
{
    public const string RateLimitPolicy = "PasswordReset";
    private const string ResetPath = "/auth/reset-password";

    /// <summary>
    /// URL absoluta de la pantalla de reset para la institución del request. Usa el dominio que
    /// resolvió el multitenant (X-Tenant u Origin): solo puede ser un dominio registrado, porque un
    /// dominio desconocido ya respondió 404 en TenantResolutionMiddleware. En local (loopback o sin
    /// dominio) usa APP_PUBLIC_URL, o el servidor de desarrollo de Angular.
    /// </summary>
    public static string ResetPageUrl(HttpRequest request, IConfiguration configuration)
    {
        var host = TenantResolutionMiddleware.RequestedHost(request);
        if (host is null || host is "localhost" or "127.0.0.1" or "::1" or "[::1]")
        {
            var publicUrl = configuration["APP_PUBLIC_URL"] ?? "http://localhost:4200";
            return publicUrl.TrimEnd('/') + ResetPath;
        }
        return $"https://{host}{ResetPath}";
    }

    /// <summary>5 pedidos de recuperación por IP cada 15 minutos (evita usar la API para spam).</summary>
    public static RateLimiterOptions AddPasswordResetPolicy(this RateLimiterOptions options)
    {
        options.AddPolicy(RateLimitPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        return options;
    }
}
