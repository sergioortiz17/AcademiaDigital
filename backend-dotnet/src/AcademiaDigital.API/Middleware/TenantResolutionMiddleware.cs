using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.Services;

namespace AcademiaDigital.API.Middleware;

/// <summary>
/// Resuelve la institución del request y la deja en <see cref="TenantContext"/>.
///
/// El dominio del frontend (el que escribió el usuario en el navegador) llega en el header
/// <c>X-Tenant</c> (lo agrega el interceptor de Angular) o, si falta, en el host del header
/// <c>Origin</c>. Sin ninguno de los dos (Swagger, scripts, tests) se usa la institución por
/// defecto. Un dominio informado pero no registrado responde 404, salvo loopback en desarrollo.
///
/// No es un mecanismo de seguridad: el dominio no es un secreto. El aislamiento lo da el hecho de
/// que cada institución tiene su propia base (usuarios, sesiones y datos no se comparten).
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public const string TenantHeader = "X-Tenant";
    private const string PlatformPath = "/api/v1/platform";

    public async Task InvokeAsync(HttpContext context, ITenantDirectory directory, TenantContext tenantContext)
    {
        if (!IsTenantScoped(context.Request.Path))
        {
            await next(context);
            return;
        }

        var host = RequestedHost(context.Request);
        TenantInfo? tenant;
        if (host is null)
        {
            tenant = directory.Default;
        }
        else
        {
            tenant = await directory.FindByHostAsync(host, context.RequestAborted);
            if (tenant is null && IsLoopback(host)) tenant = directory.Default;
        }

        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(
                new { success = false, msg = "Institución no encontrada para este dominio." }));
            return;
        }

        tenantContext.Set(tenant);
        await next(context);
    }

    /// <summary>
    /// Solo la API académica es multitenant; Swagger y la API de plataforma no tienen institución.
    /// Todo middleware que toque la base académica (p. ej. la sesión activa) debe usar este mismo criterio.
    /// </summary>
    public static bool IsTenantScoped(PathString path)
        => path.StartsWithSegments("/api") && !path.StartsWithSegments(PlatformPath);

    internal static string? RequestedHost(HttpRequest request)
    {
        var header = request.Headers[TenantHeader].ToString();
        if (!string.IsNullOrWhiteSpace(header)) return NormalizeHost(header);

        var origin = request.Headers.Origin.ToString();
        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return NormalizeHost(uri.Host);
        return null;
    }

    internal static string NormalizeHost(string value)
    {
        var host = value.Trim().ToLowerInvariant();
        var colon = host.LastIndexOf(':');
        return colon > 0 && !host.EndsWith(']') ? host[..colon] : host;
    }

    private static bool IsLoopback(string host) => host is "localhost" or "127.0.0.1" or "::1" or "[::1]";
}
