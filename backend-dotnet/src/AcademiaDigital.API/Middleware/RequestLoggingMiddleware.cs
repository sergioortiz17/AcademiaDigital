using System.Diagnostics;

namespace AcademiaDigital.API.Middleware;

/// <summary>
/// Escribe una línea de log estructurada por request (Event = "http_request") con método,
/// ruta, estado, duración, IP del cliente, usuario y rol. En producción el log sale en JSON
/// y Alloy lo envía a Loki, donde Grafana arma los paneles de accesos y errores.
///
/// Va primero en el pipeline para medir el request completo y ver el estado final que
/// devuelve ExceptionMiddleware. El usuario lo resuelve ActiveSessionMiddleware más adelante
/// en el mismo request (HttpContext.Items), por eso se lee recién al terminar.
///
/// No registra query string ni cuerpos: pueden traer tokens o datos personales.
/// </summary>
public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        finally
        {
            var status = context.Response.StatusCode;

            logger.Log(LevelFor(status),
                "{Event} {Method} {Path} {Status} {DurationMs} {Ip} {UserId} {Role} {UserAgent}",
                "http_request",
                context.Request.Method,
                context.Request.Path.Value,
                status,
                Math.Round(Stopwatch.GetElapsedTime(start).TotalMilliseconds, 1),
                context.Connection.RemoteIpAddress?.ToString(),
                context.Items.TryGetValue("UserId", out var userId) ? userId : null,
                context.Items.TryGetValue("UserRole", out var role) ? role : null,
                Truncate(context.Request.Headers.UserAgent.ToString(), 300));
        }
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
