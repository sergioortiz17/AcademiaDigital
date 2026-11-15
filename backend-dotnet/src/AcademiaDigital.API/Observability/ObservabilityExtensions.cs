using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace AcademiaDigital.API.Observability;

/// <summary>
/// Logs estructurados, IP real del cliente y reporte de errores del frontend.
/// En producción Alloy toma la salida JSON de los contenedores y la envía a Loki; Grafana
/// arma los paneles de accesos, errores y rendimiento a partir de esos campos.
/// </summary>
public static class ObservabilityExtensions
{
    public const string ClientErrorsPolicy = "ClientErrors";
    private const string TrustForwardedHeadersKey = "ReverseProxy:TrustForwardedHeaders";

    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        ApplyMinimumLevel(builder.Configuration);
        builder.Services.AddSingleton<AuditLog>();

        // Fuera de Development: una línea JSON por evento, con los placeholders del mensaje
        // como campos (Event, UserId, Ip, Status, DurationMs...).
        if (!builder.Environment.IsDevelopment())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(o =>
            {
                o.UseUtcTimestamp = true;
                o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
            });
        }

        // IP real del cliente. En AWS el request pasa por ALB → nginx (frontend) → backend.
        // El ALB agrega la IP del cliente al final de X-Forwarded-For y nginx lo reenvía tal
        // cual; se toma solo esa última entrada (ForwardLimit = 1), así un X-Forwarded-For
        // falso enviado por el cliente no cuenta. Se confía en el proxy inmediato porque en
        // ese despliegue el backend no publica puertos: solo lo alcanza nginx. Por eso se
        // activa únicamente con ReverseProxy__TrustForwardedHeaders = true (Terraform lo
        // define en Beanstalk); en local, donde el backend expone :8000, queda apagado.
        // Sin esto, el log y el rate limiter de admisiones verían la IP de nginx para todos.
        if (TrustForwardedHeaders(builder.Configuration))
        {
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
            {
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                o.ForwardLimit = 1;
                o.KnownNetworks.Clear();
                o.KnownProxies.Clear();
            });
        }

        return builder;
    }

    /// <summary>Errores reportados por el frontend: endpoint anónimo, limitado por IP.</summary>
    public static RateLimiterOptions AddClientErrorsPolicy(this RateLimiterOptions options)
    {
        options.AddPolicy(ClientErrorsPolicy, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        return options;
    }

    /// <summary>Va primero en el pipeline: normaliza la IP y registra cada request.</summary>
    public static WebApplication UseObservability(this WebApplication app)
    {
        if (TrustForwardedHeaders(app.Configuration))
            app.UseForwardedHeaders();

        app.UseMiddleware<RequestLoggingMiddleware>();
        return app;
    }

    /// <summary>
    /// Observability__MinimumLevel (por ejemplo "Error") sube el nivel mínimo de TODAS las
    /// categorías de Logging:LogLevel, incluidas las específicas de appsettings.json
    /// (Microsoft.AspNetCore, Microsoft.EntityFrameworkCore). En producción se usa "Error":
    /// solo se guardan errores. Se cambia desde Terraform sin recompilar la imagen.
    /// La auditoría de accesos (AuditLog) queda siempre en Information, salvo
    /// Observability__AuditEnabled=false.
    /// </summary>
    public static void ApplyMinimumLevel(ConfigurationManager configuration)
    {
        if (!Enum.TryParse<LogLevel>(configuration["Observability:MinimumLevel"], ignoreCase: true, out var minimum))
            return;

        var overrides = new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = minimum.ToString()
        };
        foreach (var category in configuration.GetSection("Logging:LogLevel").GetChildren())
        {
            if (Enum.TryParse<LogLevel>(category.Value, ignoreCase: true, out var level) && level < minimum)
                overrides[category.Path] = minimum.ToString();
        }

        var auditLevel = configuration.GetValue("Observability:AuditEnabled", true)
            ? LogLevel.Information
            : LogLevel.None;
        overrides[$"Logging:LogLevel:{AuditLog.Category}"] = auditLevel.ToString();

        configuration.AddInMemoryCollection(overrides);
    }

    private static bool TrustForwardedHeaders(IConfiguration configuration)
        => configuration.GetValue(TrustForwardedHeadersKey, false);
}
