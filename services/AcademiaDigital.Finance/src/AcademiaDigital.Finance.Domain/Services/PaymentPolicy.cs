using System.Text.RegularExpressions;
using AcademiaDigital.Finance.Domain.Entities;

namespace AcademiaDigital.Finance.Domain.Services;

public sealed partial class PaymentPolicy
{
    public string NormalizeDni(string value)
    {
        var normalized = DniSeparators().Replace(value?.Trim() ?? string.Empty, string.Empty);
        if (!DniDigits().IsMatch(normalized))
            throw new ArgumentException("Student DNI must contain 7 to 10 digits.");
        return normalized;
    }

    public void ValidateDraft(
        decimal amount,
        PaymentMethodKind method,
        string? externalReference,
        string? notes,
        IReadOnlyCollection<(Guid DebtPublicId, decimal Amount)> allocations)
    {
        ValidateMoney(amount, "Payment amount");
        if (allocations.Count == 0) throw new ArgumentException("At least one debt allocation is required.");
        if (allocations.GroupBy(item => item.DebtPublicId).Any(group => group.Count() > 1))
            throw new ArgumentException("A debt cannot be allocated more than once in the same payment.");
        foreach (var allocation in allocations) ValidateMoney(allocation.Amount, "Allocation amount");
        if (Round(allocations.Sum(item => item.Amount)) != amount)
            throw new ArgumentException("Payment amount must equal the sum of its allocations.");
        if (method == PaymentMethodKind.BankTransfer && string.IsNullOrWhiteSpace(externalReference))
            throw new ArgumentException("Bank transfers require an external reference.");
        if (externalReference?.Trim().Length > 100)
            throw new ArgumentException("External reference cannot exceed 100 characters.");
        if (notes?.Trim().Length > 500)
            throw new ArgumentException("Payment notes cannot exceed 500 characters.");
    }

    public string ValidateIdempotencyKey(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 8 or > 100)
            throw new ArgumentException("Idempotency-Key must contain 8 to 100 characters.");
        return normalized;
    }

    public void ValidateReconciliation(PaymentReconciliationDecision decision, string? note)
    {
        if (!Enum.IsDefined(decision)) throw new ArgumentException("Reconciliation decision is invalid.");
        if (decision == PaymentReconciliationDecision.Reject && string.IsNullOrWhiteSpace(note))
            throw new ArgumentException("Rejecting a transfer requires a note.");
        if (note?.Trim().Length > 500) throw new ArgumentException("Reconciliation note cannot exceed 500 characters.");
    }

    public string ValidateReversalReason(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < 5 or > 500)
            throw new ArgumentException("Reversal reason must contain 5 to 500 characters.");
        return normalized;
    }

    public void ApplyAllocation(StudentDebt debt, decimal amount)
    {
        ValidateMoney(amount, "Allocation amount");
        if (debt.Status == StudentDebtStatus.Cancelled) throw new InvalidOperationException("Cancelled debts cannot receive payments.");
        var outstanding = Round(debt.TotalAmount - debt.PaidAmount);
        if (amount > outstanding) throw new InvalidOperationException("Payment allocation exceeds the debt outstanding amount.");
        debt.PaidAmount = Round(debt.PaidAmount + amount);
        debt.Status = debt.PaidAmount == debt.TotalAmount ? StudentDebtStatus.Paid : StudentDebtStatus.PartiallyPaid;
    }

    public void ReverseAllocation(StudentDebt debt, decimal amount)
    {
        ValidateMoney(amount, "Reversal allocation amount");
        if (amount > debt.PaidAmount) throw new InvalidOperationException("Reversal exceeds the debt paid amount.");
        debt.PaidAmount = Round(debt.PaidAmount - amount);
        debt.Status = debt.PaidAmount == 0m ? StudentDebtStatus.Pending : StudentDebtStatus.PartiallyPaid;
    }

    private static void ValidateMoney(decimal value, string name)
    {
        if (value <= 0m) throw new ArgumentException($"{name} must be greater than zero.");
        if (value != Round(value)) throw new ArgumentException($"{name} cannot contain more than two decimal places.");
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    [GeneratedRegex("[.\\s-]+", RegexOptions.CultureInvariant)]
    private static partial Regex DniSeparators();

    [GeneratedRegex("^[0-9]{7,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex DniDigits();
}
