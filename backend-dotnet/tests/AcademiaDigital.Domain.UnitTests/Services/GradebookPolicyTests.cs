using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class GradebookPolicyTests
{
    private readonly GradebookPolicy policy = new();

    [Fact]
    public void Evaluations_must_add_up_to_one_hundred_percent()
        => Assert.Throws<ArgumentException>(() => policy.ValidateEvaluations(new[]
        {
            Evaluation("Partial", 50m),
            Evaluation("Project", 40m)
        }));

    [Fact]
    public void Evaluation_names_are_unique_case_insensitively()
        => Assert.Throws<ArgumentException>(() => policy.ValidateEvaluations(new[]
        {
            Evaluation("Partial", 50m),
            Evaluation("partial", 50m)
        }));

    [Fact]
    public void Weighted_average_is_rounded_to_two_decimals_and_promotes()
    {
        var result = policy.CalculateResult(new[]
        {
            (8m, 10m, 40m),
            (9m, 10m, 60m)
        }, new CourseApprovalRule
        {
            MinimumRegularGrade = 6m,
            MinimumPromotionGrade = 8m,
            AllowsPromotion = true
        });

        Assert.Equal(8.60m, result.Average);
        Assert.Equal(EnrollmentStatus.Promoted, result.Status);
    }

    [Fact]
    public void Promotion_requires_every_instance_above_seven_even_if_weighted_average_is_high()
    {
        // Promedio ponderado alto (8.40) pero una instancia en 6 (<= 7): NO promociona,
        // cae a Regular porque el promedio supera el mínimo regular.
        var result = policy.CalculateResult(new[]
        {
            (6m, 10m, 20m),
            (9m, 10m, 80m)
        }, new CourseApprovalRule
        {
            MinimumRegularGrade = 6m,
            MinimumPromotionGrade = 8m,
            AllowsPromotion = true
        });

        Assert.Equal(8.40m, result.Average);
        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
    }

    [Fact]
    public void Promotion_when_all_instances_above_seven_sets_status_and_average()
    {
        // Todas las instancias en 8 (> 7): promociona, con nota final = promedio (8.00).
        var result = policy.CalculateResult(new[]
        {
            (8m, 10m, 30m),
            (8m, 10m, 30m),
            (8m, 10m, 40m)
        }, new CourseApprovalRule
        {
            MinimumRegularGrade = 6m,
            MinimumPromotionGrade = 8m,
            AllowsPromotion = true
        });

        Assert.Equal(8.00m, result.Average);
        Assert.Equal(EnrollmentStatus.Promoted, result.Status);
    }

    [Fact]
    public void An_instance_exactly_at_seven_blocks_promotion()
    {
        // El umbral es estrictamente mayor a 7: una instancia en exactamente 7 bloquea la promoción.
        var result = policy.CalculateResult(new[]
        {
            (7m, 10m, 20m),
            (9m, 10m, 80m)
        }, new CourseApprovalRule
        {
            MinimumRegularGrade = 6m,
            MinimumPromotionGrade = 8m,
            AllowsPromotion = true
        });

        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
    }

    [Theory]
    [InlineData(6, EnrollmentStatus.Regularized)]
    [InlineData(5.99, EnrollmentStatus.Failed)]
    public void Result_uses_regularization_threshold(decimal average, EnrollmentStatus expected)
    {
        var result = policy.CalculateResult(
            new[] { (average, 10m, 100m) },
            new CourseApprovalRule { MinimumRegularGrade = 6m });
        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public void Approved_gradebook_is_immutable_until_reopened()
    {
        var gradebook = new Gradebook { Status = GradebookStatus.Approved };
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEditable(gradebook));
        policy.EnsureCanReopen(gradebook, "Authorized correction");
    }

    [Fact]
    public void Submission_requires_every_roster_grade()
    {
        var gradebook = new Gradebook
        {
            Status = GradebookStatus.Draft,
            Evaluations = [Evaluation("Partial", 50m), Evaluation("Project", 50m)],
            GradeRevisions = [new GradeEntryRevision { EvaluationId = 1, EnrollmentId = 1, IsCurrent = true }]
        };
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanSubmit(gradebook, 1));
    }

    private static GradebookEvaluation Evaluation(string name, decimal weight)
        => new() { Name = name, WeightPercentage = weight, MaximumScore = 10m };
}
