using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using AcademiaDigital.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Finance.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(FinanceDbContext db) : IPaymentRepository
{
    public async Task<IReadOnlyList<PaymentMethod>> GetActiveMethodsAsync(CancellationToken ct = default)
        => await db.PaymentMethods.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToArrayAsync(ct);

    public Task<PaymentMethod?> FindActiveMethodAsync(int id, CancellationToken ct = default)
        => db.PaymentMethods.SingleOrDefaultAsync(item => item.Id == id && item.IsActive, ct);

    public async Task<IReadOnlyList<StudentDebt>> GetDebtsByPublicIdsAsync(IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
        => await db.StudentDebts.Where(item => publicIds.Contains(item.PublicId)).OrderBy(item => item.Id).ToArrayAsync(ct);

    public void AddPayment(Payment payment) => db.Payments.Add(payment);

    public Task<Payment?> FindByConfirmationKeyForUpdateAsync(string idempotencyKey, CancellationToken ct = default)
        => Graph(db.Payments
            .FromSqlInterpolated($"SELECT * FROM finance.\"Payments\" WHERE confirmation_idempotency_key = {idempotencyKey} FOR UPDATE"))
            .SingleOrDefaultAsync(ct);

    public Task<Payment?> FindForUpdateAsync(Guid publicId, CancellationToken ct = default)
        => Graph(db.Payments
            .FromSqlInterpolated($"SELECT * FROM finance.\"Payments\" WHERE public_id = {publicId} FOR UPDATE"))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StudentDebt>> LockDebtsForPaymentAsync(long paymentId, CancellationToken ct = default)
    {
        var debts = await db.StudentDebts
            .FromSqlInterpolated($"SELECT d.* FROM finance.\"StudentDebts\" d WHERE EXISTS (SELECT 1 FROM finance.\"PaymentAllocations\" a WHERE a.payment_id = {paymentId} AND a.student_debt_id = d.\"Id\") FOR UPDATE OF d")
            .OrderBy(item => item.Id)
            .ToArrayAsync(ct);
        foreach (var debt in debts) await db.Entry(debt).ReloadAsync(ct);
        return debts;
    }

    public async Task<IReadOnlyList<Payment>> GetByStudentAsync(long studentId, CancellationToken ct = default)
        => await Graph(db.Payments.AsNoTracking().Where(item => item.StudentId == studentId))
            .OrderByDescending(item => item.CreatedAt).ToArrayAsync(ct);

    private static IQueryable<Payment> Graph(IQueryable<Payment> query)
        => query
            .Include(item => item.PaymentMethod)
            .Include(item => item.Allocations).ThenInclude(item => item.StudentDebt).ThenInclude(item => item.FinancialConcept)
            .Include(item => item.Reconciliations)
            .Include(item => item.Reversals)
            .Include(item => item.Receipt);
}
