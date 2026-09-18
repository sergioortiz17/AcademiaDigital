using System.Text.Json.Serialization;

namespace AcademiaDigital.Finance.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethodKind
{
    Cash = 0,
    BankTransfer = 1,
    DebitCard = 2,
    CreditCard = 3
}

public sealed class PaymentMethod
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PaymentMethodKind Kind { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Draft = 0,
    PendingReconciliation = 1,
    Confirmed = 2,
    Rejected = 3,
    Reversed = 4
}

public sealed class Payment
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public string? ConfirmationIdempotencyKey { get; set; }
    // Student navigation cut on extraction — StudentId stays. The student's display
    // name and DNI are captured as an immutable snapshot at creation time so receipts
    // and payment listings never depend on a live call into the monolith.
    public long StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentDni { get; set; } = string.Empty;
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public string Currency { get; set; } = "ARS";
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;
    public string? ExternalReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    // CreatedByUser navigation cut on extraction — CreatedByUserId stays.
    public long CreatedByUserId { get; set; }
    public DateTime? ConfirmationRequestedAt { get; set; }
    // ConfirmationRequestedByUser navigation cut on extraction — id stays.
    public long? ConfirmationRequestedByUserId { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    // ConfirmedByUser navigation cut on extraction — id stays.
    public long? ConfirmedByUserId { get; set; }
    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
    public ICollection<PaymentReconciliation> Reconciliations { get; set; } = [];
    public ICollection<PaymentReversal> Reversals { get; set; } = [];
    public Receipt? Receipt { get; set; }
}

public sealed class PaymentAllocation
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public long StudentDebtId { get; set; }
    public StudentDebt StudentDebt { get; set; } = null!;
    public decimal Amount { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentReconciliationDecision
{
    Approve = 0,
    Reject = 1
}

public sealed class PaymentReconciliation
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public PaymentReconciliationDecision Decision { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    // CreatedByUser navigation cut on extraction — CreatedByUserId stays.
    public long CreatedByUserId { get; set; }
}

public sealed class PaymentReversal
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // CreatedByUser navigation cut on extraction — CreatedByUserId stays.
    public long CreatedByUserId { get; set; }
}
