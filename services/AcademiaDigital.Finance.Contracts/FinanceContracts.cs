namespace AcademiaDigital.Finance.Contracts;

/// <summary>
/// Contrato HTTP entre el monolito y el microservicio Finance (ADR 0001).
///
/// Son objetos PLANOS de transporte: sin lógica de negocio, sin referencias a entidades de
/// dominio de ningún lado. Se comparten por ProjectReference desde el monolito (cliente) y
/// desde Finance (servidor) para no duplicar a mano la forma del payload. Los enums de dominio
/// (p. ej. la condición del alumno) viajan como string para no acoplar el contrato a los enums
/// internos de ninguno de los dos.
/// </summary>

/// <summary>
/// Request: el monolito le pide a Finance generar la deuda de matriculación de un alumno.
/// POST /api/v1/finance/debts/generate (header Idempotency-Key aparte).
/// </summary>
public sealed record GenerateDebtRequest(
    long StudentId,
    int CareerId,
    long StudentCareerId,
    long BillingPlanId,
    int AcademicYear,
    string? Condition = null,
    IReadOnlyCollection<int>? GrantedScholarshipIds = null);

/// <summary>Response de la generación de deuda.</summary>
public sealed record GenerateDebtResponse(
    Guid BatchId,
    int GeneratedDebtCount,
    decimal TotalAmount);

/// <summary>
/// Response liviano del estado de deuda de un alumno (solo informativo — Finance NUNCA
/// bloquea el flujo académico). GET /api/v1/finance/students/{studentId}/debts/summary.
/// </summary>
public sealed record StudentDebtStatusResponse(
    long StudentId,
    decimal TotalOwed,
    int OverdueCount);
