using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class ReceiptRepository(AppDbContext db) : IReceiptRepository
{
    public Task<ReceiptSequence> LockSequenceAsync(CancellationToken ct = default)
        => db.ReceiptSequences
            .FromSqlRaw("SELECT * FROM \"ReceiptSequences\" WHERE id = 1 FOR UPDATE")
            .SingleAsync(ct);

    public Task<Receipt?> FindByPaymentAsync(long paymentId, bool tracking, CancellationToken ct = default)
        => Query(tracking).SingleOrDefaultAsync(item => item.PaymentId == paymentId, ct);

    public Task<Receipt?> FindByPublicIdAsync(Guid publicId, bool tracking, CancellationToken ct = default)
        => Query(tracking).SingleOrDefaultAsync(item => item.PublicId == publicId, ct);

    public async Task<IReadOnlyList<Receipt>> GetByStudentAsync(long studentId, CancellationToken ct = default)
        => await Query(false).Where(item => item.Payment.StudentId == studentId)
            .OrderByDescending(item => item.SequenceNumber).ToArrayAsync(ct);

    public async Task<IReadOnlyList<Receipt>> GetByUserAsync(long userId, CancellationToken ct = default)
        => await Query(false).Where(item => item.Payment.Student.UserId == userId)
            .OrderByDescending(item => item.SequenceNumber).ToArrayAsync(ct);

    public void Add(Receipt receipt) => db.Receipts.Add(receipt);

    private IQueryable<Receipt> Query(bool tracking)
    {
        IQueryable<Receipt> query = db.Receipts
            .Include(item => item.Payment)
                .ThenInclude(item => item.Student)
                    .ThenInclude(item => item.User);
        return tracking ? query : query.AsNoTracking();
    }
}
