using System.Data;
using AcademiaDigital.Finance.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Finance.Infrastructure.Persistence;

public sealed class UnitOfWork(FinanceDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default)
        => await ExecuteAsync(operation, null, ct);

    public async Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
        => await ExecuteAsync(operation, IsolationLevel.Serializable, ct);

    private async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        IsolationLevel? isolationLevel,
        CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null)
            return await operation(ct);

        await using var transaction = isolationLevel.HasValue
            ? await db.Database.BeginTransactionAsync(isolationLevel.Value, ct)
            : await db.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            db.ChangeTracker.Clear();
            throw;
        }
    }
}
