using System.Text.Json.Serialization;

namespace AcademiaDigital.Finance.Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExpenseEntryKind
{
    Expense = 0,
    Reversal = 1
}

public sealed class ExpenseCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedByUserId { get; set; }
}

public sealed class ExpenseEntry
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public ExpenseEntryKind Kind { get; set; }
    public int CategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public long? ReversalOfExpenseId { get; set; }
    public ExpenseEntry? ReversalOfExpense { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ARS";
    public string? PurchaseNote { get; set; }
    public string? Observation { get; set; }
    public string? AttachmentStorageKey { get; set; }
    public string? AttachmentFileName { get; set; }
    public string? AttachmentContentType { get; set; }
    public string? AttachmentSha256 { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
