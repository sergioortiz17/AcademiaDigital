using AcademiaDigital.Finance.Domain.Entities;

namespace AcademiaDigital.Finance.Domain.Interfaces.Repositories;

public sealed record ExpenseFilter(DateOnly? From, DateOnly? To, int? CategoryId, string? Supplier);

public interface IExpenseRepository
{
    Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(bool activeOnly, CancellationToken ct = default);
    Task<ExpenseCategory?> FindCategoryAsync(int id, bool tracking, CancellationToken ct = default);
    Task<bool> CategoryCodeExistsAsync(string code, CancellationToken ct = default);
    void AddCategory(ExpenseCategory category);
    Task<IReadOnlyList<ExpenseEntry>> GetEntriesAsync(ExpenseFilter filter, CancellationToken ct = default);
    Task<ExpenseEntry?> FindEntryAsync(Guid publicId, bool tracking, CancellationToken ct = default);
    Task<bool> HasReversalAsync(long expenseId, CancellationToken ct = default);
    void AddEntry(ExpenseEntry entry);
}
