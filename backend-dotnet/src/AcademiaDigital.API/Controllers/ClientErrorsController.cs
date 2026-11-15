using AcademiaDigital.API.Observability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AcademiaDigital.API.Controllers;

/// <summary>
/// Recibe los errores de JavaScript del frontend (ErrorHandler global de Angular) y los
/// registra en el log del backend (Event = "client_error"), para verlos en Grafana junto
/// con los errores del servidor. Es anónimo porque un error puede ocurrir antes del login;
/// lo limita un rate limiter por IP y un tamaño máximo de cuerpo.
/// </summary>
[Route("api/v1/client-errors")]
public class ClientErrorsController(ILogger<ClientErrorsController> logger) : ApiControllerBase
{
    [HttpPost]
    [EnableRateLimiting(ObservabilityExtensions.ClientErrorsPolicy)]
    [RequestSizeLimit(16_384)]
    public IActionResult Report([FromBody] ClientErrorReport report)
    {
        // Error y no Warning: en producción solo se guardan los errores.
        logger.LogError(
            "{Event} {ErrorMessage} {Url} {Stack} {UserId} {Role}",
            "client_error",
            Truncate(report.Message, 1_000),
            Truncate(report.Url, 500),
            Truncate(report.Stack, 4_000),
            CurrentUserId,
            CurrentUserRole is { } r ? (int)r : null);

        return Accepted();
    }

    private static string? Truncate(string? value, int max)
        => value is null || value.Length <= max ? value : value[..max];
}

// Sin [MaxLength]: un error largo igual se registra (truncado), en lugar de responder 400.
public record ClientErrorReport(string? Message, string? Stack, string? Url);
