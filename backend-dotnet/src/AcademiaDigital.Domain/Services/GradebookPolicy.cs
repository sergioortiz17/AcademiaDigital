using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed record GradebookResult(decimal Average, EnrollmentStatus Status);

public sealed class GradebookPolicy
{
    public void EnsureCanCreate(TeachingPosition position, IReadOnlyCollection<GradebookEvaluation> evaluations)
    {
        if (!position.IsActive || !position.CommissionId.HasValue)
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
        if (evaluations.Any(item => item.WeightPercentage <= 0m || item.WeightPercentage > 100m))
            throw new ArgumentException("Los pesos de las evaluaciones deben ser mayores que cero y no mayores que cien.");
        if (evaluations.Any(item => item.MaximumScore <= 0m || item.MaximumScore > 100m))
            throw new ArgumentException("Los puntajes máximos de las evaluaciones deben ser mayores que cero y no mayores que cien.");
        if (evaluations.Sum(item => item.WeightPercentage) != 100m)
            throw new ArgumentException("Los pesos de las evaluaciones deben sumar exactamente cien por ciento.");
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
        var current = gradebook.GradeRevisions.Where(item => item.IsCurrent).ToArray();
        if (rosterCount == 0)
            throw new InvalidOperationException("Una planilla sin estudiantes inscriptos no puede enviarse.");
        if (current.Select(item => (item.EvaluationId, item.EnrollmentId)).Distinct().Count()
            != rosterCount * gradebook.Evaluations.Count)
            throw new InvalidOperationException("Cada estudiante inscripto requiere una nota para cada evaluación antes del envío.");
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

    public GradebookResult CalculateResult(
        IReadOnlyCollection<(decimal Score, decimal MaximumScore, decimal WeightPercentage)> grades,
        CourseApprovalRule? rule)
    {
        if (grades.Count == 0 || grades.Sum(item => item.WeightPercentage) != 100m)
            throw new InvalidOperationException("Se requiere un conjunto completo de notas ponderadas.");
        var average = decimal.Round(grades.Sum(item =>
            item.Score / item.MaximumScore * 10m * item.WeightPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        var minimumRegular = rule?.MinimumRegularGrade ?? 6m;
        var promotion = rule?.AllowsPromotion == true
            && rule.MinimumPromotionGrade.HasValue
            && average >= rule.MinimumPromotionGrade.Value;
        return new GradebookResult(
            average,
            promotion ? EnrollmentStatus.Promoted
                : average >= minimumRegular ? EnrollmentStatus.Regularized
                : EnrollmentStatus.Failed);
    }
}
