using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Finance.Infrastructure.Persistence.Repositories;

public sealed class ExpenseRepository(FinanceDbContext db) : IExpenseRepository
{
    public async Task<IReadOnlyList<ExpenseCategory>> GetCategoriesAsync(bool activeOnly, CancellationToken ct = default)
    {
        var query = db.ExpenseCategories.AsNoTracking().AsQueryable();
        if (activeOnly) query = query.Where(item => item.IsActive);
        return await query.OrderBy(item => item.Name).ToArrayAsync(ct);
    }

    public Task<ExpenseCategory?> FindCategoryAsync(int id, bool tracking, CancellationToken ct = default)
    {
        var query = db.ExpenseCategories.AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.Id == id, ct);
    }

    public Task<bool> CategoryCodeExistsAsync(string code, CancellationToken ct = default)
        => db.ExpenseCategories.AnyAsync(item => item.Code == code, ct);

    public void AddCategory(ExpenseCategory category) => db.ExpenseCategories.Add(category);

    public async Task<IReadOnlyList<ExpenseEntry>> GetEntriesAsync(ExpenseFilter filter, CancellationToken ct = default)
    {
        var query = db.ExpenseEntries.AsNoTracking().Include(item => item.Category).Include(item => item.ReversalOfExpense).AsQueryable();
        if (filter.From.HasValue) query = query.Where(item => item.ExpenseDate >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(item => item.ExpenseDate <= filter.To.Value);
        if (filter.CategoryId.HasValue) query = query.Where(item => item.CategoryId == filter.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Supplier))
        {
            var supplier = filter.Supplier.Trim().ToLower();
            query = query.Where(item => item.SupplierName.ToLower().Contains(supplier));
        }
        return await query.OrderByDescending(item => item.ExpenseDate).ThenByDescending(item => item.Id).ToArrayAsync(ct);
    }

    public Task<ExpenseEntry?> FindEntryAsync(Guid publicId, bool tracking, CancellationToken ct = default)
    {
        var query = db.ExpenseEntries.Include(item => item.Category).Include(item => item.ReversalOfExpense).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.PublicId == publicId, ct);
    }

    public Task<bool> HasReversalAsync(long expenseId, CancellationToken ct = default)
        => db.ExpenseEntries.AnyAsync(item => item.ReversalOfExpenseId == expenseId, ct);

    public void AddEntry(ExpenseEntry entry) => db.ExpenseEntries.Add(entry);
}
