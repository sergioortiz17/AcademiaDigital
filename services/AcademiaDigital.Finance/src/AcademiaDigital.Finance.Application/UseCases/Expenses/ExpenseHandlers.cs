using System.Security.Cryptography;
using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Finance.Application.UseCases.Expenses;

public sealed record CreateExpenseCategoryCommand(string Code, string Name, long ActorUserId);
public sealed record SetExpenseCategoryStatusCommand(int Id, bool IsActive);
public sealed record RegisterExpenseCommand(
    int CategoryId, string SupplierName, DateOnly ExpenseDate, decimal Amount,
    string? PurchaseNote, string? Observation, byte[]? Attachment,
    string? AttachmentContentType, string? AttachmentFileName, long ActorUserId);
public sealed record ReverseExpenseCommand(Guid PublicId, string Reason, long ActorUserId);
public sealed record ExpenseCategoryDto(int Id, string Code, string Name, bool IsActive);
public sealed record ExpenseEntryDto(
    Guid PublicId, ExpenseEntryKind Kind, int CategoryId, string Category, Guid? ReversalOf,
    string SupplierName, DateOnly ExpenseDate, decimal Amount, string Currency,
    string? PurchaseNote, string? Observation, bool HasAttachment, string? AttachmentSha256,
    long CreatedByUserId, DateTime CreatedAtUtc);
public sealed record ExpenseLedgerDto(decimal ExpenseTotal, decimal ReversalTotal, decimal NetTotal, IReadOnlyList<ExpenseEntryDto> Entries);

public sealed class CreateExpenseCategoryCommandHandler(
    IExpenseRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<ExpenseCategoryDto> Handle(CreateExpenseCategoryCommand command, CancellationToken ct = default)
    {
        var code = Normalize(command.Code, 30, "Category code").ToUpperInvariant();
        var name = Normalize(command.Name, 120, "Category name");
        if (await repository.CategoryCodeExistsAsync(code, ct)) throw new InvalidOperationException("Expense category code already exists.");
        var category = new ExpenseCategory
        {
            Code = code, Name = name, IsActive = true, CreatedByUserId = command.ActorUserId,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        repository.AddCategory(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Map(category);
    }

    internal static ExpenseCategoryDto Map(ExpenseCategory category) => new(category.Id, category.Code, category.Name, category.IsActive);
    internal static string Normalize(string value, int max, string name)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 || normalized.Length > max) throw new ArgumentException($"{name} is required and cannot exceed {max} characters.");
        return normalized;
    }
}

public sealed class SetExpenseCategoryStatusCommandHandler(IExpenseRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<ExpenseCategoryDto> Handle(SetExpenseCategoryStatusCommand command, CancellationToken ct = default)
    {
        var category = await repository.FindCategoryAsync(command.Id, true, ct) ?? throw new KeyNotFoundException("Expense category not found.");
        category.IsActive = command.IsActive;
        await unitOfWork.SaveChangesAsync(ct);
        return CreateExpenseCategoryCommandHandler.Map(category);
    }
}

public sealed class GetExpenseCategoriesQueryHandler(IExpenseRepository repository)
{
    public async Task<IReadOnlyList<ExpenseCategoryDto>> Handle(bool activeOnly, CancellationToken ct = default)
        => (await repository.GetCategoriesAsync(activeOnly, ct)).Select(CreateExpenseCategoryCommandHandler.Map).ToArray();
}

public sealed class RegisterExpenseCommandHandler(
    IExpenseRepository repository, IFileStorage storage, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    private const int MaxAttachmentBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        { "application/pdf", "image/png", "image/jpeg" };

    public async Task<ExpenseEntryDto> Handle(RegisterExpenseCommand command, CancellationToken ct = default)
    {
        var category = await repository.FindCategoryAsync(command.CategoryId, false, ct);
        if (category is not { IsActive: true }) throw new KeyNotFoundException("Active expense category not found.");
        if (command.Amount <= 0) throw new ArgumentException("Expense amount must be greater than zero.");
        var publicId = Guid.NewGuid();
        string? key = null;
        string? sha = null;
        if (command.Attachment is not null)
        {
            ValidateAttachment(command.Attachment, command.AttachmentContentType, command.AttachmentFileName);
            sha = Convert.ToHexString(SHA256.HashData(command.Attachment)).ToLowerInvariant();
            var extension = Path.GetExtension(command.AttachmentFileName!).ToLowerInvariant();
            key = $"expenses/{command.ExpenseDate.Year}/{publicId:N}/attachment{extension}";
            await storage.SaveAsync(key, command.Attachment, command.AttachmentContentType!, command.AttachmentFileName!, ct);
        }
        var entry = new ExpenseEntry
        {
            PublicId = publicId, Kind = ExpenseEntryKind.Expense, CategoryId = command.CategoryId,
            SupplierName = CreateExpenseCategoryCommandHandler.Normalize(command.SupplierName, 200, "Supplier"),
            ExpenseDate = command.ExpenseDate, Amount = decimal.Round(command.Amount, 2), Currency = "ARS",
            PurchaseNote = Optional(command.PurchaseNote, 100), Observation = Optional(command.Observation, 1000),
            AttachmentStorageKey = key, AttachmentFileName = command.AttachmentFileName,
            AttachmentContentType = command.AttachmentContentType, AttachmentSha256 = sha,
            CreatedByUserId = command.ActorUserId, CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };
        repository.AddEntry(entry);
        await unitOfWork.SaveChangesAsync(ct);
        entry.Category = category;
        return Map(entry);
    }

    private static void ValidateAttachment(byte[] content, string? contentType, string? fileName)
    {
        if (content.Length is 0 or > MaxAttachmentBytes) throw new ArgumentException("Attachment must contain between 1 byte and 10 MB.");
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName) throw new ArgumentException("Attachment file name is invalid.");
        if (contentType is null || !AllowedContentTypes.Contains(contentType)) throw new ArgumentException("Attachment must be PDF, PNG or JPEG.");
        var validSignature = contentType.ToLowerInvariant() switch
        {
            "application/pdf" => content.AsSpan().StartsWith("%PDF-"u8),
            "image/png" => content.AsSpan().StartsWith(new byte[] { 0x89, 0x50, 0x4e, 0x47 }),
            "image/jpeg" => content.AsSpan().StartsWith(new byte[] { 0xff, 0xd8, 0xff }),
            _ => false
        };
        if (!validSignature) throw new ArgumentException("Attachment content does not match its MIME type.");
    }

    internal static ExpenseEntryDto Map(ExpenseEntry item) => new(item.PublicId, item.Kind, item.CategoryId,
        item.Category.Name, item.ReversalOfExpense?.PublicId, item.SupplierName, item.ExpenseDate, item.Amount,
        item.Currency, item.PurchaseNote, item.Observation, item.AttachmentStorageKey is not null,
        item.AttachmentSha256, item.CreatedByUserId, item.CreatedAtUtc);

    private static string? Optional(string? value, int max)
    {
        var normalized = value?.Trim();
        if (normalized?.Length > max) throw new ArgumentException($"Value cannot exceed {max} characters.");
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}

public sealed class ReverseExpenseCommandHandler(
    IExpenseRepository repository, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public Task<ExpenseEntryDto> Handle(ReverseExpenseCommand command, CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var original = await repository.FindEntryAsync(command.PublicId, false, transactionCt)
                ?? throw new KeyNotFoundException("Expense not found.");
            if (original.Kind != ExpenseEntryKind.Expense) throw new InvalidOperationException("Only an expense entry can be reversed.");
            if (await repository.HasReversalAsync(original.Id, transactionCt)) throw new InvalidOperationException("Expense was already reversed.");
            var reversal = new ExpenseEntry
            {
                PublicId = Guid.NewGuid(), Kind = ExpenseEntryKind.Reversal, CategoryId = original.CategoryId,
                ReversalOfExpenseId = original.Id,
                SupplierName = original.SupplierName, ExpenseDate = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime),
                Amount = original.Amount, Currency = original.Currency,
                Observation = CreateExpenseCategoryCommandHandler.Normalize(command.Reason, 1000, "Reversal reason"),
                CreatedByUserId = command.ActorUserId, CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
            };
            repository.AddEntry(reversal);
            await unitOfWork.SaveChangesAsync(transactionCt);
            // The original and its category were intentionally queried without tracking.
            // Attach the navigations only after persisting the compensating entry so EF
            // cannot attempt to insert the existing graph again.
            reversal.ReversalOfExpense = original;
            reversal.Category = original.Category;
            return RegisterExpenseCommandHandler.Map(reversal);
        }, ct);
}

public sealed class GetExpensesQueryHandler(IExpenseRepository repository)
{
    public async Task<ExpenseLedgerDto> Handle(ExpenseFilter filter, CancellationToken ct = default)
    {
        if (filter.From.HasValue && filter.To.HasValue && filter.To < filter.From) throw new ArgumentException("The end date cannot precede the start date.");
        var entries = await repository.GetEntriesAsync(filter, ct);
        var expenses = entries.Where(item => item.Kind == ExpenseEntryKind.Expense).Sum(item => item.Amount);
        var reversals = entries.Where(item => item.Kind == ExpenseEntryKind.Reversal).Sum(item => item.Amount);
        return new(expenses, reversals, expenses - reversals, entries.Select(RegisterExpenseCommandHandler.Map).ToArray());
    }
}

public sealed class DownloadExpenseAttachmentQueryHandler(IExpenseRepository repository, IFileStorage storage)
{
    public async Task<StoredFile> Handle(Guid publicId, CancellationToken ct = default)
    {
        var entry = await repository.FindEntryAsync(publicId, false, ct) ?? throw new KeyNotFoundException("Expense not found.");
        if (entry.AttachmentStorageKey is null) throw new KeyNotFoundException("Expense attachment not found.");
        return await storage.ReadAsync(entry.AttachmentStorageKey, entry.AttachmentContentType!, entry.AttachmentFileName!, ct)
            ?? throw new KeyNotFoundException("Expense attachment file not found.");
    }
}
