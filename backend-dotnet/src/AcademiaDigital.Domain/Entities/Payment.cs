using System.Text.Json.Serialization;

namespace AcademiaDigital.Domain.Entities;

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
    public long StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public string Currency { get; set; } = "ARS";
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Draft;
    public string? ExternalReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime? ConfirmationRequestedAt { get; set; }
    public long? ConfirmationRequestedByUserId { get; set; }
    public User? ConfirmationRequestedByUser { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public User? ConfirmedByUser { get; set; }
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
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
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
    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
