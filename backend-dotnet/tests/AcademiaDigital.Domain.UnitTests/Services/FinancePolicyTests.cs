using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class FinancePolicyTests
{
    private readonly FinancePolicy policy = new();

    [Theory]
    [InlineData(" cuota_2026 ", "CUOTA_2026")]
    [InlineData("matricula-anual", "MATRICULA-ANUAL")]
    public void Concept_codes_are_normalized(string source, string expected)
        => Assert.Equal(expected, policy.NormalizeCode(source));

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("CUOTA CON ESPACIO")]
    public void Invalid_concept_codes_are_rejected(string code)
        => Assert.Throws<ArgumentException>(() => policy.NormalizeCode(code));

    [Fact]
    public void Surcharge_is_applied_before_the_most_beneficial_discount()
    {
        var result = policy.Calculate(
            1000m,
            new DateOnly(2026, 8, 20),
            new DateOnly(2026, 8, 24),
            10m,
            [
                new(1, "GENERAL", "General", FinancialBenefitKind.Discount, 10m),
                new(2, "BECA", "Beca", FinancialBenefitKind.Scholarship, 30m)
            ]);

        Assert.Equal(100m, result.SurchargeAmount);
        Assert.Equal(330m, result.DiscountAmount);
        Assert.Equal(770m, result.TotalAmount);
        Assert.Equal(2, result.AppliedBenefit!.Id);
    }

    [Fact]
    public void Debt_before_due_date_has_no_surcharge()
    {
        var result = policy.Calculate(1234.56m, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 24), 15m, []);
        Assert.Equal(0m, result.SurchargeAmount);
        Assert.Equal(1234.56m, result.TotalAmount);
    }

    [Fact]
    public void Equal_benefits_prefer_the_scholarship()
    {
        var result = policy.Calculate(
            1000m,
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 24),
            0m,
            [
                new(1, "DISCOUNT", "Descuento", FinancialBenefitKind.Discount, 20m),
                new(2, "SCHOLARSHIP", "Beca", FinancialBenefitKind.Scholarship, 20m)
            ]);

        Assert.Equal(2, result.AppliedBenefit!.Id);
        Assert.Equal(200m, result.DiscountAmount);
    }

    [Fact]
    public void Monetary_results_round_half_away_from_zero()
    {
        var result = policy.Calculate(
            1m,
            new DateOnly(2026, 8, 24),
            new DateOnly(2026, 8, 24),
            0m,
            [new(1, "DISCOUNT", "Descuento", FinancialBenefitKind.Discount, 12.5m)]);

        Assert.Equal(0.13m, result.DiscountAmount);
        Assert.Equal(0.87m, result.TotalAmount);
    }

    [Fact]
    public void Scholarship_benefit_requires_a_scholarship_reference()
        => Assert.Throws<ArgumentException>(() => policy.ValidateBenefit(
            FinancialBenefitKind.Scholarship, null, 50m, null, null));

    [Fact]
    public void General_discount_cannot_reference_a_scholarship()
        => Assert.Throws<ArgumentException>(() => policy.ValidateBenefit(
            FinancialBenefitKind.Discount, 4, 20m, null, null));

    [Fact]
    public void Unknown_student_condition_is_rejected()
        => Assert.Throws<ArgumentException>(() => policy.ValidateStudentCondition((StudentStatus)999));

    [Fact]
    public void Plan_rejects_duplicate_concept_and_installment()
        => Assert.Throws<ArgumentException>(() => policy.ValidatePlan(
            "Plan 2026", 2026,
            [(1, 1, new DateOnly(2026, 3, 10)), (1, 1, new DateOnly(2026, 4, 10))]));
}
