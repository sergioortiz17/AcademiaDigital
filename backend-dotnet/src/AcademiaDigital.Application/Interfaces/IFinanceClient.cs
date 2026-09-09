namespace AcademiaDigital.Application.Interfaces;

/// <summary>
/// Cliente del microservicio Finance (ver ADR 0001). El monolito lo usa para REGISTRAR la deuda
/// de matriculación de un alumno como asiento contable.
///
/// CONTRATO CLAVE: es fire-and-forget y tolerante a error. Finance NUNCA bloquea el flujo
/// académico: si el servicio está caído o rechaza la solicitud, la matriculación se completa igual
/// (el error se loguea, no se propaga). La deuda se puede generar/reconciliar después.
/// </summary>
public interface IFinanceClient
{
    /// <summary>
    /// Solicita a Finance generar la deuda de matriculación. No lanza excepción ante fallas de red
    /// o del servicio: devuelve <c>false</c> y loguea. Devuelve <c>true</c> si Finance la registró.
    /// </summary>
    Task<bool> TryGenerateMatriculationDebtsAsync(GenerateMatriculationDebtsRequest request, CancellationToken ct = default);
}

/// <summary>
/// Datos mínimos para pedir la generación de deuda. El monolito envía solo ids (no entidades):
/// Finance no comparte tablas con el monolito. <see cref="BillingPlanId"/> es opcional porque puede
/// no haber un plan de facturación configurado todavía; en ese caso el monolito no llama y lo loguea.
/// </summary>
public sealed record GenerateMatriculationDebtsRequest(
    long StudentId,
    int CareerId,
    long StudentCareerId,
    int AcademicYear,
    long? BillingPlanId,
    long ActorUserId);
