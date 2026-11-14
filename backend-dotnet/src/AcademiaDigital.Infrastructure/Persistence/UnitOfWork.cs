using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Exceptions;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AcademiaDigital.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext db) : IUnitOfWork
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

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            await using var transaction = isolationLevel.HasValue
                ? await db.Database.BeginTransactionAsync(isolationLevel.Value, ct)
                : await db.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (Exception exception) when (IsSerializationFailure(exception) && attempt < maxAttempts)
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
            }
            catch (Exception exception) when (IsSerializationFailure(exception))
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                throw new ConcurrencyConflictException(
                    "La operacion no pudo completarse porque otro proceso modifico los mismos datos.",
                    exception);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                db.ChangeTracker.Clear();
                throw;
            }
        }

        throw new InvalidOperationException("The transaction retry loop ended unexpectedly.");
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure })
                return true;

        return false;
    }
}
