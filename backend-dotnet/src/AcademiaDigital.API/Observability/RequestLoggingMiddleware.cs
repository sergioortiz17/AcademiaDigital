using System.Diagnostics;

namespace AcademiaDigital.API.Observability;

/// <summary>
/// Una línea de log estructurada por request (Event = "http_request") con método, ruta,
/// estado, duración, IP del cliente, usuario y rol. El nivel depende del estado: 5xx Error,
/// 4xx Warning, resto Information. En producción (Observability__MinimumLevel=Error) solo
/// se guardan los 5xx; el resto se descarta sin costo.
///
/// Además alimenta la auditoría de accesos (AuditLog, siempre activa): login fallido y
/// logout. El login exitoso lo registra AuthController, que conoce al usuario.
///
/// Va primero en el pipeline para medir el request completo y ver el estado final que
/// devuelve ExceptionMiddleware. El usuario lo resuelve ActiveSessionMiddleware más adelante
/// en el mismo request (HttpContext.Items), por eso se lee recién al terminar.
///
/// No registra query string ni cuerpos: pueden traer tokens o datos personales.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string LoginPath = "/api/v1/users/login";
    private const string LogoutPath = "/api/v1/users/logout";

    public async Task InvokeAsync(HttpContext context, AuditLog audit)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var status = context.Response.StatusCode;
            var path = context.Request.Path.Value;
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var userAgent = Truncate(context.Request.Headers.UserAgent.ToString(), 300);
            var userId = context.Items.TryGetValue("UserId", out var uid) ? uid : null;

            logger.Log(LevelFor(status),
                "{Event} {Method} {Path} {Status} {DurationMs} {Ip} {UserId} {Role} {UserAgent}",
                "http_request",
                context.Request.Method,
                path,
                status,
                Math.Round(Stopwatch.GetElapsedTime(start).TotalMilliseconds, 1),
                ip,
                userId,
                context.Items.TryGetValue("UserRole", out var role) ? role : null,
                userAgent);

            if (HttpMethods.IsPost(context.Request.Method))
                Audit(audit, path, status, ip, userAgent, userId);
        }
    }

    private static void Audit(AuditLog audit, string? path, int status, string? ip, string userAgent, object? userId)
    {
        if (string.Equals(path, LoginPath, StringComparison.OrdinalIgnoreCase) && status >= 400)
            audit.LoginFailed(status, ip, userAgent);
        else if (string.Equals(path, LogoutPath, StringComparison.OrdinalIgnoreCase) && status < 400)
            audit.LoggedOut(userId, ip);
    }

    private static LogLevel LevelFor(int status)
    {
        if (status >= 500) return LogLevel.Error;
        if (status >= 400) return LogLevel.Warning;
        return LogLevel.Information;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
