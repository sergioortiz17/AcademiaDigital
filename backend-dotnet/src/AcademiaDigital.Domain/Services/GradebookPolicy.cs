using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed record GradebookResult(decimal Average, EnrollmentStatus Status);

public sealed class GradebookPolicy
{
    public void EnsureCanCreate(TeachingPosition position, IReadOnlyCollection<GradebookEvaluation> evaluations)
    {
        if (!position.IsActive || !position.CommissionId.HasValue)
            throw new InvalidOperationException("The teaching position must be active and assigned to a commission.");
        ValidateEvaluations(evaluations);
    }

    public void ValidateEvaluations(IReadOnlyCollection<GradebookEvaluation> evaluations)
    {
        if (evaluations.Count == 0 || evaluations.Count > 20)
            throw new ArgumentException("A gradebook requires between one and twenty evaluations.");
        if (evaluations.Any(item => string.IsNullOrWhiteSpace(item.Name) || item.Name.Trim().Length > 150))
            throw new ArgumentException("Every evaluation requires a name of up to 150 characters.");
        if (evaluations.Select(item => item.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != evaluations.Count)
            throw new ArgumentException("Evaluation names must be unique within the gradebook.");
        if (evaluations.Any(item => item.WeightPercentage <= 0m || item.WeightPercentage > 100m))
            throw new ArgumentException("Evaluation weights must be greater than zero and no greater than one hundred.");
        if (evaluations.Any(item => item.MaximumScore <= 0m || item.MaximumScore > 100m))
            throw new ArgumentException("Evaluation maximum scores must be greater than zero and no greater than one hundred.");
        if (evaluations.Sum(item => item.WeightPercentage) != 100m)
            throw new ArgumentException("Evaluation weights must add up to exactly one hundred percent.");
    }

    public void EnsureEditable(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Draft)
            throw new InvalidOperationException("Only a draft gradebook can be edited.");
    }

    public void EnsureScoreIsValid(GradebookEvaluation evaluation, decimal score)
    {
        if (score < 0m || score > evaluation.MaximumScore)
            throw new ArgumentException($"The score for '{evaluation.Name}' must be between zero and {evaluation.MaximumScore}.");
    }

    public void EnsureCanSubmit(Gradebook gradebook, int rosterCount)
    {
        EnsureEditable(gradebook);
        var current = gradebook.GradeRevisions.Where(item => item.IsCurrent).ToArray();
        if (rosterCount == 0)
            throw new InvalidOperationException("A gradebook without enrolled students cannot be submitted.");
        if (current.Select(item => (item.EvaluationId, item.EnrollmentId)).Distinct().Count()
            != rosterCount * gradebook.Evaluations.Count)
            throw new InvalidOperationException("Every enrolled student requires a grade for every evaluation before submission.");
    }

    public void EnsureCanApprove(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Submitted)
            throw new InvalidOperationException("Only a submitted gradebook can be approved.");
    }

    public void EnsureCanPublish(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Approved)
            throw new InvalidOperationException("Only an approved gradebook can be published.");
    }

    public void EnsureCanClose(Gradebook gradebook)
    {
        if (gradebook.Status != GradebookStatus.Published)
            throw new InvalidOperationException("Only a published gradebook can be closed.");
    }

    public void EnsureCanReopen(Gradebook gradebook, string reason)
    {
        if (gradebook.Status == GradebookStatus.Draft)
            throw new InvalidOperationException("A draft gradebook does not require reopening.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3 || reason.Trim().Length > 1000)
            throw new ArgumentException("A reopening reason between 3 and 1000 characters is required.");
    }

    public GradebookResult CalculateResult(
        IReadOnlyCollection<(decimal Score, decimal MaximumScore, decimal WeightPercentage)> grades,
        CourseApprovalRule? rule)
    {
        if (grades.Count == 0 || grades.Sum(item => item.WeightPercentage) != 100m)
            throw new InvalidOperationException("A complete weighted grade set is required.");
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
