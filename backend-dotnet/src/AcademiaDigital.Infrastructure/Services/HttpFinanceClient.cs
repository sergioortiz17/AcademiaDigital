using System.Net.Http.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Finance.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AcademiaDigital.Infrastructure.Services;

/// <summary>
/// Implementación HTTP de <see cref="IFinanceClient"/> contra el microservicio Finance.
///
/// Fire-and-forget y tolerante a error por diseño (ADR 0001): cualquier falla (red, timeout,
/// 4xx/5xx, o falta de BillingPlan) se loguea y devuelve false; NUNCA lanza. Así la matriculación
/// del monolito no puede ser bloqueada por Finance.
///
/// La identidad se reenvía por headers (X-User-Id/X-User-Role) — Finance confía en el borde
/// interno y no re-autentica. La base URL viene de configuración (Finance:BaseUrl).
/// </summary>
public sealed class HttpFinanceClient(HttpClient http, IConfiguration configuration, ILogger<HttpFinanceClient> logger)
    : IFinanceClient
{
    public async Task<bool> TryGenerateMatriculationDebtsAsync(
        GenerateMatriculationDebtsRequest request, CancellationToken ct = default)
    {
        // Sin plan de facturación configurado no hay nada que generar (Finance lo exige).
        // No es un error: se loguea y se sigue. La deuda se podrá generar cuando exista un plan.
        if (request.BillingPlanId is null)
        {
            logger.LogInformation(
                "Finance: matriculación de student {StudentId} sin BillingPlan configurado; no se genera deuda (fire-and-forget).",
                request.StudentId);
            return false;
        }

        var baseUrl = configuration["Finance:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("Finance: Finance:BaseUrl no configurada; se omite la generación de deuda.");
            return false;
        }

        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/api/v1/finance/debts/generate")
            {
                // Payload tipado por el contrato compartido (AcademiaDigital.Finance.Contracts),
                // no un objeto anónimo: monolito y Finance comparten la MISMA forma del request.
                Content = JsonContent.Create(new GenerateDebtRequest(
                    StudentId: request.StudentId,
                    CareerId: request.CareerId,
                    StudentCareerId: request.StudentCareerId,
                    BillingPlanId: request.BillingPlanId!.Value,
                    AcademicYear: request.AcademicYear))
            };
            // Identidad forwardeada (Finance confía en el borde interno).
            message.Headers.Add("X-User-Id", request.ActorUserId.ToString());
            message.Headers.Add("X-User-Role", "Admin");
            // Idempotencia: reintentos no duplican la deuda de esta matriculación.
            message.Headers.Add("Idempotency-Key", $"matricula-{request.StudentCareerId}-{request.AcademicYear}");

            using var response = await http.SendAsync(message, ct);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Finance: deuda de matriculación generada para student {StudentId}.", request.StudentId);
                return true;
            }

            logger.LogWarning("Finance: /debts/generate respondió {Status} para student {StudentId}; se ignora (fire-and-forget).",
                (int)response.StatusCode, request.StudentId);
            return false;
        }
        catch (Exception ex)
        {
            // Tolerante a error: NUNCA propaga. La matriculación ya se completó.
            logger.LogWarning(ex, "Finance: falló la llamada a /debts/generate para student {StudentId}; se ignora (fire-and-forget).",
                request.StudentId);
            return false;
        }
    }
}
