using System.Net.Http.Headers;
using System.Text.Json;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.API.Middleware;

/// <summary>
/// Mantiene las rutas públicas de Finance bajo el monolito. El borde valida la sesión,
/// descarta cualquier identidad aportada por el cliente y reenvía únicamente la identidad
/// resuelta por <see cref="ActiveSessionMiddleware"/>.
/// </summary>
public sealed class FinanceGatewayMiddleware(RequestDelegate next)
{
    private static readonly string[] FinancePrefixes =
    [
        "/api/v1/finance",
        "/api/v1/payments",
        "/api/v1/receipts"
    ];

    private static readonly HashSet<string> ResponseHeadersToSkip = new(StringComparer.OrdinalIgnoreCase)
    {
        "connection",
        "content-length",
        "keep-alive",
        "proxy-authenticate",
        "proxy-authorization",
        "te",
        "trailer",
        "transfer-encoding",
        "upgrade"
    };

    public async Task InvokeAsync(
        HttpContext context,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<FinanceGatewayMiddleware> logger)
    {
        if (!IsFinanceRequest(context.Request.Path))
        {
            await next(context);
            return;
        }

        if (!TryGetTrustedIdentity(context, out var userId, out var role))
        {
            await WriteProblemAsync(context, StatusCodes.Status401Unauthorized,
                "Se requiere una sesión activa para acceder a Finanzas.");
            return;
        }

        var baseUrl = configuration["Finance:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogError("Finance gateway is enabled but Finance:BaseUrl is not configured.");
            await WriteUnavailableAsync(context);
            return;
        }

        var destination = $"{baseUrl.TrimEnd('/')}{context.Request.Path}{context.Request.QueryString}";
        using var outgoing = new HttpRequestMessage(new HttpMethod(context.Request.Method), destination);

        if (RequestCanHaveBody(context.Request))
        {
            outgoing.Content = new StreamContent(context.Request.Body);
            if (MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var contentType))
                outgoing.Content.Headers.ContentType = contentType;
        }

        CopyRequestHeader(context, outgoing, "Accept");
        CopyRequestHeader(context, outgoing, "Idempotency-Key");
        outgoing.Headers.TryAddWithoutValidation("X-User-Id", userId.ToString());
        outgoing.Headers.TryAddWithoutValidation("X-User-Role", role.ToString());
        if (context.Items.TryGetValue("StudentId", out var studentIdValue)
            && studentIdValue is long studentId
            && studentId > 0)
            outgoing.Headers.TryAddWithoutValidation("X-Student-Id", studentId.ToString());

        try
        {
            var client = httpClientFactory.CreateClient("FinanceGateway");
            using var response = await client.SendAsync(
                outgoing,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            context.Response.StatusCode = (int)response.StatusCode;
            CopyResponseHeaders(response, context.Response);
            if (response.Content is not null)
                await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning("Finance gateway timed out for {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteUnavailableAsync(context);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Finance gateway is unavailable for {Method} {Path}.", context.Request.Method, context.Request.Path);
            await WriteUnavailableAsync(context);
        }
    }

    private static bool IsFinanceRequest(PathString path)
        => FinancePrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));

    private static bool TryGetTrustedIdentity(HttpContext context, out long userId, out UserRole role)
    {
        userId = 0;
        role = default;
        return context.Items.TryGetValue("UserId", out var idValue)
            && idValue is long resolvedUserId
            && context.Items.TryGetValue("UserRole", out var roleValue)
            && roleValue is int resolvedRole
            && Enum.IsDefined(typeof(UserRole), resolvedRole)
            && (userId = resolvedUserId) > 0
            && Enum.TryParse(resolvedRole.ToString(), out role);
    }

    private static bool RequestCanHaveBody(HttpRequest request)
        => request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding");

    private static void CopyRequestHeader(HttpContext context, HttpRequestMessage outgoing, string name)
    {
        if (context.Request.Headers.TryGetValue(name, out var values))
            outgoing.Headers.TryAddWithoutValidation(name, values.ToArray());
    }

    private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse destination)
    {
        foreach (var header in source.Headers.Concat(source.Content.Headers))
        {
            if (!ResponseHeadersToSkip.Contains(header.Key))
                destination.Headers[header.Key] = header.Value.ToArray();
        }
    }

    private static Task WriteUnavailableAsync(HttpContext context)
        => WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable,
            "Finanzas no está disponible temporalmente. La operación académica puede continuar y se puede reintentar más tarde.");

    private static async Task WriteProblemAsync(HttpContext context, int status, string detail)
    {
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            type = "about:blank",
            title = status == StatusCodes.Status503ServiceUnavailable
                ? "Servicio financiero no disponible"
                : "No autorizado",
            status,
            detail
        }, cancellationToken: context.RequestAborted);
    }
}
