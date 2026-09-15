using AcademiaDigital.Finance.Domain.Entities;

namespace AcademiaDigital.Finance.Domain.Services;

public sealed record FinancialBenefitCandidate(
    long Id,
    string Code,
    string Name,
    FinancialBenefitKind Kind,
    decimal Percentage);

public sealed record FinancialCalculation(
    decimal BaseAmount,
    decimal SurchargeAmount,
    decimal DiscountAmount,
    decimal TotalAmount,
    FinancialBenefitCandidate? AppliedBenefit);

public sealed class FinancePolicy
{
    public string NormalizeCode(string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 30 || normalized.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
            throw new ArgumentException("Code must contain 2 to 30 letters, numbers, dashes or underscores.");
        return normalized;
    }

    public void ValidateRate(decimal amount, decimal surchargePercentage, int academicYear)
    {
        if (amount <= 0m) throw new ArgumentException("Rate amount must be greater than zero.");
        ValidatePercentage(surchargePercentage, nameof(surchargePercentage));
        if (academicYear is < 2000 or > 2200) throw new ArgumentException("Academic year is invalid.");
    }

    public void ValidateStudentCondition(StudentStatus? condition)
    {
        if (condition.HasValue && !Enum.IsDefined(condition.Value))
            throw new ArgumentException("Student condition is invalid.");
    }

    public void ValidateBenefit(
        FinancialBenefitKind kind,
        int? scholarshipId,
        decimal percentage,
        DateOnly? validFrom,
        DateOnly? validTo)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentException("Benefit kind is invalid.");
        ValidatePercentage(percentage, nameof(percentage), allowZero: false);
        if (kind == FinancialBenefitKind.Scholarship && !scholarshipId.HasValue)
            throw new ArgumentException("Scholarship benefits require a scholarship.");
        if (kind == FinancialBenefitKind.Discount && scholarshipId.HasValue)
            throw new ArgumentException("General discounts cannot reference a scholarship.");
        if (validFrom.HasValue && validTo.HasValue && validTo < validFrom)
            throw new ArgumentException("Benefit validity range is invalid.");
    }

    public void ValidatePlan(string name, int academicYear, IReadOnlyCollection<(int ConceptId, int InstallmentNumber, DateOnly DueDate)> items)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150)
            throw new ArgumentException("Plan name is required and cannot exceed 150 characters.");
        if (academicYear is < 2000 or > 2200) throw new ArgumentException("Academic year is invalid.");
        if (items.Count == 0) throw new ArgumentException("A billing plan requires at least one item.");
        if (items.Any(item => item.ConceptId <= 0 || item.InstallmentNumber <= 0))
            throw new ArgumentException("Plan items require a concept and a positive installment number.");
        if (items.GroupBy(item => (item.ConceptId, item.InstallmentNumber)).Any(group => group.Count() > 1))
            throw new ArgumentException("Plan items cannot repeat concept and installment number.");
    }

    public FinancialCalculation Calculate(
        decimal baseAmount,
        DateOnly dueDate,
        DateOnly calculationDate,
        decimal surchargePercentage,
        IReadOnlyCollection<FinancialBenefitCandidate> benefits)
    {
        ValidateRate(baseAmount, surchargePercentage, calculationDate.Year);
        var surcharge = calculationDate > dueDate
            ? Round(baseAmount * surchargePercentage / 100m)
            : 0m;
        var subtotal = Round(baseAmount + surcharge);
        var best = benefits
            .OrderByDescending(benefit => Round(subtotal * benefit.Percentage / 100m))
            .ThenByDescending(benefit => benefit.Kind == FinancialBenefitKind.Scholarship)
            .ThenBy(benefit => benefit.Code, StringComparer.Ordinal)
            .FirstOrDefault();
        var discount = best is null ? 0m : Round(subtotal * best.Percentage / 100m);
        return new(baseAmount, surcharge, discount, Round(subtotal - discount), best);
    }

    private static void ValidatePercentage(decimal value, string name, bool allowZero = true)
    {
        if ((allowZero && value < 0m) || value > 100m)
            throw new ArgumentException($"{name} must be between 0 and 100.");
        if (!allowZero && (value <= 0m || value > 100m))
            throw new ArgumentException($"{name} must be greater than 0 and at most 100.");
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
