namespace AcademiaDigital.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default);
}
