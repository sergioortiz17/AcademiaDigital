using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

/// <summary>
/// Nota de una evaluación para el cálculo de condición. Las instancias regulares (IsRecovery=false)
/// llevan peso; las recuperaciones (IsRecovery=true) no aportan peso propio: por valor reemplazan a
/// la instancia regular desaprobada más baja. HasScore=false = todavía sin nota cargada.
/// </summary>
public readonly record struct EvaluationScore(
    bool IsRecovery, bool HasScore, decimal Score, decimal MaximumScore, decimal WeightPercentage);

/// <summary>
/// Resultado del cálculo. Pending=true cuando no se puede cerrar la condición todavía (queda alguna
/// instancia regular sin nota que ninguna recuperación cubre). En ese caso Average/Status son null.
/// </summary>
public sealed record GradebookResult(decimal? Average, EnrollmentStatus? Status, bool Pending)
{
    public static GradebookResult PendingResult() => new(null, null, true);
}

public sealed class GradebookPolicy
{
    // Una instancia regular se considera "aprobada" (no necesita recuperación) si su nota normalizada
    // a escala 0-10 es estrictamente mayor a este umbral. Es un umbral aparte del mínimo para
    // regularizar/promocionar; solo decide si esa instancia puntual requiere recuperación.
    public const decimal InstancePassThreshold = 4m;

    // Para Promocionado, TODAS las notas que efectivamente cuentan (instancias, con sus reemplazos por
    // recuperación) deben superar este umbral. No se toca: es sobre el umbral, no sobre cuáles cuentan.
    public const decimal PromotionPerInstanceThreshold = 7m;

    public void EnsureCanCreate(CourseSection position, IReadOnlyCollection<GradebookEvaluation> evaluations)
    {
        if (!position.IsActive || !position.DivisionId.HasValue)
            throw new InvalidOperationException("El cargo docente debe estar activo y asignado a una comisión.");
        ValidateEvaluations(evaluations);
    }

    public void ValidateEvaluations(IReadOnlyCollection<GradebookEvaluation> evaluations)
    {
        if (evaluations.Count == 0 || evaluations.Count > 20)
            throw new ArgumentException("Una planilla requiere entre una y veinte evaluaciones.");
        if (evaluations.Any(item => string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 150))
            throw new ArgumentException("Cada evaluación requiere un nombre de hasta 150 caracteres.");
        if (evaluations.Select(item => item.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != evaluations.Count)
            throw new ArgumentException("Los nombres de las evaluaciones deben ser únicos dentro de la planilla.");
        if (evaluations.Any(item => item.MaximumScore <= 0m || item.MaximumScore > 100m))
            throw new ArgumentException("Los puntajes máximos de las evaluaciones deben ser mayores que cero y no mayores que cien.");

        // Instancias regulares: peso individual en (0, 100]. Recuperaciones: sin peso propio (0),
        // porque por valor heredan el de la instancia que reemplazan.
        if (evaluations.Any(item => !item.IsRecovery && (item.WeightPercentage <= 0m || item.WeightPercentage > 100m)))
            throw new ArgumentException("Los pesos de las instancias regulares deben ser mayores que cero y no mayores que cien.");
        if (evaluations.Any(item => item.IsRecovery && item.WeightPercentage != 0m))
            throw new ArgumentException("Las recuperaciones no llevan peso propio (deben tener peso cero).");

        // Solo las instancias regulares deben sumar 100%: las recuperaciones no llevan peso propio.
        // Si no hay recuperaciones, esto equivale al 100% clásico sobre todas.
        var regular = evaluations.Where(item => !item.IsRecovery).ToArray();
        if (regular.Length == 0)
            throw new ArgumentException("La planilla requiere al menos una instancia regular (no recuperación).");
        if (regular.Sum(item => item.WeightPercentage) != 100m)
            throw new ArgumentException("Los pesos de las instancias regulares deben sumar exactamente cien por ciento.");
    }

    public void EnsureEditable(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Draft)
            throw new InvalidOperationException("Solo una planilla en borrador puede editarse.");
    }

    public void EnsureScoreIsValid(GradebookEvaluation evaluation, decimal score)
    {
        if (score < 0m || score > evaluation.MaximumScore)
            throw new ArgumentException($"El puntaje de '{evaluation.Name}' debe estar entre cero y {evaluation.MaximumScore}.");
    }

    public void EnsureCanSubmit(Gradebook gradebook, int rosterCount)
    {
        EnsureEditable(gradebook);
        if (rosterCount == 0)
            throw new InvalidOperationException("Una planilla sin estudiantes inscriptos no puede enviarse.");

        var regularEvaluationIds = gradebook.Evaluations.Where(e => !e.IsRecovery).Select(e => e.Id).ToHashSet();
        if (regularEvaluationIds.Count == 0)
            throw new InvalidOperationException("La planilla no tiene instancias regulares configuradas.");

        // Basta con que cada alumno tenga nota en TODAS las instancias regulares. Las recuperaciones
        // son opcionales: quien aprobó sus instancias no necesita cargarlas para poder enviar.
        var currentRegular = gradebook.GradeRevisions
            .Where(item => item.IsCurrent && regularEvaluationIds.Contains(item.EvaluationId))
            .Select(item => (item.EvaluationId, item.EnrollmentId))
            .Distinct()
            .Count();
        if (currentRegular != rosterCount * regularEvaluationIds.Count)
            throw new InvalidOperationException("Cada estudiante inscripto requiere una nota en todas las instancias regulares antes del envío.");
    }

    public void EnsureCanApprove(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Submitted)
            throw new InvalidOperationException("Solo se puede aprobar una planilla enviada.");
    }

    public void EnsureCanPublish(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Approved)
            throw new InvalidOperationException("Solo se puede publicar una planilla aprobada.");
    }

    public void EnsureCanClose(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Published)
            throw new InvalidOperationException("Solo se puede cerrar una planilla publicada.");
    }

    public void EnsureCanReopen(Gradebook gradebook, string reason)
    {
        if (gradebook.Status == GradebookStatus.Draft)
            throw new InvalidOperationException("Una planilla en borrador no requiere reapertura.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3 || reason.Trim().Length > 1000)
            throw new ArgumentException("Se requiere un motivo de reapertura de entre 3 y 1000 caracteres.");
    }

    /// <summary>
    /// Calcula promedio y condición a partir de las notas de las evaluaciones de un alumno,
    /// aplicando recuperaciones por valor: cada recuperación cargada reemplaza la nota de la
    /// instancia regular desaprobada más baja (las ausentes/ sin nota se priorizan como las peores).
    /// El promedio sale de las instancias regulares (con sus reemplazos), con sus pesos originales.
    /// Si tras aplicar recuperaciones queda alguna instancia regular sin nota, devuelve Pending.
    /// </summary>
    public GradebookResult CalculateResult(IReadOnlyCollection<EvaluationScore> evaluations, CourseApprovalRule? rule)
    {
        var instances = evaluations.Where(e => !e.IsRecovery).ToList();
        if (instances.Count == 0 || instances.Sum(e => e.WeightPercentage) != 100m)
            throw new InvalidOperationException("Las instancias regulares deben tener pesos que sumen cien por ciento.");

        // Nota normalizada 0-10 de cada instancia (null si todavía no tiene nota cargada).
        var slots = instances
            .Select(e => new InstanceSlot(
                Normalized: e.HasScore ? e.Score / e.MaximumScore * 10m : (decimal?)null,
                WeightPercentage: e.WeightPercentage))
            .ToList();

        // Recuperaciones cargadas, como notas normalizadas 0-10 disponibles para cubrir instancias.
        var recoveryScores = evaluations
            .Where(e => e.IsRecovery && e.HasScore)
            .Select(e => e.Score / e.MaximumScore * 10m)
            .OrderByDescending(v => v) // usar la mejor recuperación para la peor instancia
            .ToList();

        // Instancias que necesitan recuperación: sin nota (las peores) o desaprobadas (<= umbral),
        // ordenadas de peor a mejor. Se les asigna la mejor recuperación disponible, en orden.
        var needy = slots
            .Select((slot, idx) => (slot, idx))
            .Where(x => x.slot.Normalized is null || x.slot.Normalized.Value <= InstancePassThreshold)
            .OrderBy(x => x.slot.Normalized ?? decimal.MinValue) // sin nota = lo más bajo
            .ToList();

        var recoveryIndex = 0;
        foreach (var (_, idx) in needy)
        {
            if (recoveryIndex >= recoveryScores.Count) break;
            slots[idx] = slots[idx] with { Normalized = recoveryScores[recoveryIndex] };
            recoveryIndex++;
        }

        // Opción I: si tras aplicar recuperaciones queda una instancia sin nota, la condición queda
        // pendiente (no se puede cerrar todavía). El caso "ausente = desaprobó" será una tarea aparte.
        if (slots.Any(s => s.Normalized is null))
            return GradebookResult.PendingResult();

        var average = decimal.Round(
            slots.Sum(s => s.Normalized!.Value * s.WeightPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        var minimumRegular = rule?.MinimumRegularGrade ?? 6m;

        // Para promocionar: además del flag y del mínimo de promoción, TODAS las notas que cuentan
        // (instancias, ya con reemplazos por recuperación) deben superar el umbral de promoción.
        var allCountAbovePromotion = slots.All(s => s.Normalized!.Value > PromotionPerInstanceThreshold);

        var promotion = rule?.AllowsPromotion == true
            && rule.MinimumPromotionGrade.HasValue
            && average >= rule.MinimumPromotionGrade.Value
            && allCountAbovePromotion;

        var status = promotion ? EnrollmentStatus.Promoted
            : average >= minimumRegular ? EnrollmentStatus.Regularized
            : EnrollmentStatus.Failed;
        return new GradebookResult(average, status, Pending: false);
    }

    private readonly record struct InstanceSlot(decimal? Normalized, decimal WeightPercentage);
}
