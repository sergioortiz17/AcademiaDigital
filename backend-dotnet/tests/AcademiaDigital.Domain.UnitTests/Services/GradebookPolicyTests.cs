using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class GradebookPolicyTests
{
    private readonly GradebookPolicy policy = new();

    // Helpers para armar EvaluationScore: Reg = instancia regular (lleva peso), Rec = recuperación.
    private static EvaluationScore Reg(decimal score, decimal weight, decimal max = 10m)
        => new(IsRecovery: false, HasScore: true, Score: score, MaximumScore: max, WeightPercentage: weight);
    private static EvaluationScore RegEmpty(decimal weight, decimal max = 10m)
        => new(IsRecovery: false, HasScore: false, Score: 0m, MaximumScore: max, WeightPercentage: weight);
    private static EvaluationScore Rec(decimal score, decimal max = 10m)
        => new(IsRecovery: true, HasScore: true, Score: score, MaximumScore: max, WeightPercentage: 0m);
    private static EvaluationScore RecEmpty(decimal max = 10m)
        => new(IsRecovery: true, HasScore: false, Score: 0m, MaximumScore: max, WeightPercentage: 0m);

    private static CourseApprovalRule Rule(decimal regular = 6m, decimal? promotion = null, bool allowsPromotion = false)
        => new() { MinimumRegularGrade = regular, MinimumPromotionGrade = promotion, AllowsPromotion = allowsPromotion };

    [Fact]
    public void Regular_instance_weights_must_add_up_to_one_hundred_percent()
        => Assert.Throws<ArgumentException>(() => policy.ValidateEvaluations(new[]
        {
            Evaluation("Partial", 50m),
            Evaluation("Project", 40m)
        }));

    [Fact]
    public void Recovery_evaluations_do_not_count_towards_the_hundred_percent()
    {
        // 3 instancias 40/30/30 = 100 y dos recuperaciones sin peso: válido.
        policy.ValidateEvaluations(new[]
        {
            Evaluation("1era", 40m),
            Evaluation("2da", 30m),
            Evaluation("3era", 30m),
            Recovery("Recuperación 1"),
            Recovery("Recuperación 2")
        });
    }

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
        var result = policy.CalculateResult(new[] { Reg(8m, 40m), Reg(9m, 60m) },
            Rule(promotion: 8m, allowsPromotion: true));

        Assert.False(result.Pending);
        Assert.Equal(8.60m, result.Average);
        Assert.Equal(EnrollmentStatus.Promoted, result.Status);
    }

    [Fact]
    public void Promotion_requires_every_instance_above_seven_even_if_weighted_average_is_high()
    {
        // Promedio ponderado alto (8.40) pero una instancia en 6 (<= 7): NO promociona, cae a Regular.
        var result = policy.CalculateResult(new[] { Reg(6m, 20m), Reg(9m, 80m) },
            Rule(promotion: 8m, allowsPromotion: true));

        Assert.Equal(8.40m, result.Average);
        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
    }

    [Fact]
    public void Promotion_when_all_instances_above_seven_sets_status_and_average()
    {
        var result = policy.CalculateResult(new[] { Reg(8m, 30m), Reg(8m, 30m), Reg(8m, 40m) },
            Rule(promotion: 8m, allowsPromotion: true));

        Assert.Equal(8.00m, result.Average);
        Assert.Equal(EnrollmentStatus.Promoted, result.Status);
    }

    [Fact]
    public void An_instance_exactly_at_seven_blocks_promotion()
    {
        var result = policy.CalculateResult(new[] { Reg(7m, 20m), Reg(9m, 80m) },
            Rule(promotion: 8m, allowsPromotion: true));

        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
    }

    [Theory]
    [InlineData(6, EnrollmentStatus.Regularized)]
    [InlineData(5.99, EnrollmentStatus.Failed)]
    public void Result_uses_regularization_threshold(decimal average, EnrollmentStatus expected)
    {
        var result = policy.CalculateResult(new[] { Reg(average, 100m) }, Rule(regular: 6m));
        Assert.Equal(expected, result.Status);
    }

    // ── Problema 2: recuperaciones opcionales / dinámicas ───────────────────────

    [Fact]
    public void All_regular_instances_passed_ignores_empty_recoveries_and_computes()
    {
        // 1era/2da/3era en 8/8/9 y recuperaciones SIN cargar: calcula igual (no pendiente).
        var result = policy.CalculateResult(new[]
        {
            Reg(8m, 34m), Reg(8m, 33m), Reg(9m, 33m), RecEmpty(), RecEmpty()
        }, Rule(promotion: 7m, allowsPromotion: true));

        Assert.False(result.Pending);
        Assert.Equal(EnrollmentStatus.Promoted, result.Status);
    }

    [Fact]
    public void Missing_regular_instance_without_recovery_is_pending()
    {
        // 1era desaprobada implícita: falta nota en 1era y no hay recuperación cargada -> pendiente.
        var result = policy.CalculateResult(new[]
        {
            RegEmpty(34m), Reg(8m, 33m), Reg(9m, 33m), RecEmpty(), RecEmpty()
        }, Rule());

        Assert.True(result.Pending);
        Assert.Null(result.Status);
        Assert.Null(result.Average);
    }

    [Fact]
    public void Recovery_replaces_the_missing_instance_and_closes_condition()
    {
        // 1era sin nota (necesita recuperación); Recuperación 1 = 7 la cubre -> ya no pendiente.
        var result = policy.CalculateResult(new[]
        {
            RegEmpty(34m), Reg(8m, 33m), Reg(9m, 33m), Rec(7m), RecEmpty()
        }, Rule(regular: 6m));

        Assert.False(result.Pending);
        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
    }

    [Fact]
    public void Recovery_replaces_the_lowest_failed_instance()
    {
        // 1era = 3 (desaprobada, <= 4), resto aprobado; Recuperación = 8 reemplaza a la 1era.
        // Promedio con 8/8/9 aprox alto; regla regular 6 -> Regularized (no promociona por regla off).
        var result = policy.CalculateResult(new[]
        {
            Reg(3m, 34m), Reg(8m, 33m), Reg(9m, 33m), Rec(8m)
        }, Rule(regular: 6m));

        Assert.False(result.Pending);
        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
        // La 1era pasó de 3 a 8: promedio ~ 8.34, claramente >= 6.
        Assert.True(result.Average >= 8m);
    }

    [Fact]
    public void Instance_pass_threshold_is_strictly_above_four()
    {
        // Una instancia en exactamente 4 se considera desaprobada (necesita recuperación).
        // Sin recuperación cargada pero CON nota, no es pendiente: se calcula con la nota baja.
        var result = policy.CalculateResult(new[]
        {
            Reg(4m, 34m), Reg(8m, 33m), Reg(9m, 33m)
        }, Rule(regular: 6m));

        Assert.False(result.Pending);
        // Promedio ~ (4+8+9)/3 ≈ 7.02 -> Regularized (>=6), pero no promociona (regla off).
        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
    }

    [Fact]
    public void Two_recoveries_cover_two_failed_instances_worst_first()
    {
        // 1era=2 y 2da=3 desaprobadas; 3era=9. Recuperaciones 6 y 7: la mejor (7) cubre la peor (2).
        var result = policy.CalculateResult(new[]
        {
            Reg(2m, 34m), Reg(3m, 33m), Reg(9m, 33m), Rec(6m), Rec(7m)
        }, Rule(regular: 6m));

        Assert.False(result.Pending);
        // Tras reemplazo: 1era=7, 2da=6, 3era=9 -> promedio ~ 7.33 -> Regularized.
        Assert.Equal(EnrollmentStatus.Regularized, result.Status);
        Assert.True(result.Average >= 6m);
    }

    [Fact]
    public void Approved_gradebook_is_immutable_until_reopened()
    {
        var gradebook = new Gradebook { Status = GradebookStatus.Approved };
        Assert.Throws<InvalidOperationException>(() => policy.EnsureEditable(gradebook));
        policy.EnsureCanReopen(gradebook, "Authorized correction");
    }

    [Fact]
    public void Submission_requires_every_regular_instance_grade()
    {
        var gradebook = new Gradebook
        {
            Status = GradebookStatus.Draft,
            Evaluations =
            [
                new GradebookEvaluation { Id = 1, Name = "1era", WeightPercentage = 50m, MaximumScore = 10m },
                new GradebookEvaluation { Id = 2, Name = "2da", WeightPercentage = 50m, MaximumScore = 10m }
            ],
            GradeRevisions = [new GradeEntryRevision { EvaluationId = 1, EnrollmentId = 1, IsCurrent = true }]
        };
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanSubmit(gradebook, 1));
    }

    [Fact]
    public void Submission_allowed_with_empty_recoveries_when_regular_instances_are_graded()
    {
        // Instancias regulares (1,2,3) cargadas; recuperaciones (4,5) vacías -> se puede enviar.
        var gradebook = new Gradebook
        {
            Status = GradebookStatus.Draft,
            Evaluations =
            [
                new GradebookEvaluation { Id = 1, Name = "1era", WeightPercentage = 34m, MaximumScore = 10m },
                new GradebookEvaluation { Id = 2, Name = "2da", WeightPercentage = 33m, MaximumScore = 10m },
                new GradebookEvaluation { Id = 3, Name = "3era", WeightPercentage = 33m, MaximumScore = 10m },
                new GradebookEvaluation { Id = 4, Name = "Recuperación 1", WeightPercentage = 0m, MaximumScore = 10m, IsRecovery = true },
                new GradebookEvaluation { Id = 5, Name = "Recuperación 2", WeightPercentage = 0m, MaximumScore = 10m, IsRecovery = true }
            ],
            GradeRevisions =
            [
                new GradeEntryRevision { EvaluationId = 1, EnrollmentId = 1, IsCurrent = true, Score = 8m },
                new GradeEntryRevision { EvaluationId = 2, EnrollmentId = 1, IsCurrent = true, Score = 8m },
                new GradeEntryRevision { EvaluationId = 3, EnrollmentId = 1, IsCurrent = true, Score = 9m }
            ]
        };
        // No lanza: las recuperaciones vacías no bloquean el envío.
        policy.EnsureCanSubmit(gradebook, 1);
    }

    private static GradebookEvaluation Evaluation(string name, decimal weight)
        => new() { Name = name, WeightPercentage = weight, MaximumScore = 10m };
    private static GradebookEvaluation Recovery(string name)
        => new() { Name = name, WeightPercentage = 0m, MaximumScore = 10m, IsRecovery = true };
}
