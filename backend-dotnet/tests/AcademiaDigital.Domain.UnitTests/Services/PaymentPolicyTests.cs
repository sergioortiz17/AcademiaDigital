using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class PaymentPolicyTests
{
    private readonly PaymentPolicy policy = new();

    [Theory]
    [InlineData("12.345.678", "12345678")]
    [InlineData(" 12-345-678 ", "12345678")]
    public void Dni_is_normalized(string source, string expected)
        => Assert.Equal(expected, policy.NormalizeDni(source));

    [Theory]
    [InlineData("123")]
    [InlineData("ABC12345")]
    public void Invalid_dni_is_rejected(string value)
        => Assert.Throws<ArgumentException>(() => policy.NormalizeDni(value));

    [Fact]
    public void Draft_requires_allocations_to_equal_payment_amount()
        => Assert.Throws<ArgumentException>(() => policy.ValidateDraft(
            100m, PaymentMethodKind.Cash, null, null, [(Guid.NewGuid(), 99m)]));

    [Fact]
    public void Draft_rejects_duplicate_debt_allocations()
    {
        var debt = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => policy.ValidateDraft(
            100m, PaymentMethodKind.Cash, null, null, [(debt, 50m), (debt, 50m)]));
    }

    [Fact]
    public void Transfer_requires_external_reference()
        => Assert.Throws<ArgumentException>(() => policy.ValidateDraft(
            100m, PaymentMethodKind.BankTransfer, null, null, [(Guid.NewGuid(), 100m)]));

    [Fact]
    public void Money_rejects_more_than_two_decimal_places()
        => Assert.Throws<ArgumentException>(() => policy.ValidateDraft(
            10.001m, PaymentMethodKind.Cash, null, null, [(Guid.NewGuid(), 10.001m)]));

    [Fact]
    public void Allocation_moves_debt_from_pending_to_partial_and_paid()
    {
        var debt = Debt();
        policy.ApplyAllocation(debt, 40m);
        Assert.Equal(40m, debt.PaidAmount);
        Assert.Equal(StudentDebtStatus.PartiallyPaid, debt.Status);

        policy.ApplyAllocation(debt, 60m);
        Assert.Equal(100m, debt.PaidAmount);
        Assert.Equal(StudentDebtStatus.Paid, debt.Status);
    }

    [Fact]
    public void Allocation_rejects_overpayment()
        => Assert.Throws<InvalidOperationException>(() => policy.ApplyAllocation(Debt(), 100.01m));

    [Fact]
    public void Reversal_restores_outstanding_debt_state()
    {
        var debt = Debt();
        policy.ApplyAllocation(debt, 70m);
        policy.ReverseAllocation(debt, 70m);
        Assert.Equal(0m, debt.PaidAmount);
        Assert.Equal(StudentDebtStatus.Pending, debt.Status);
    }

    [Fact]
    public void Rejecting_transfer_requires_note()
        => Assert.Throws<ArgumentException>(() => policy.ValidateReconciliation(
            PaymentReconciliationDecision.Reject, null));

    [Fact]
    public void Reversal_requires_meaningful_reason()
        => Assert.Throws<ArgumentException>(() => policy.ValidateReversalReason("bad"));

    private static StudentDebt Debt() => new() { TotalAmount = 100m, PaidAmount = 0m, Status = StudentDebtStatus.Pending };
}
