using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository(AppDbContext db) : IPaymentRepository
{
    public async Task<IReadOnlyList<PaymentMethod>> GetActiveMethodsAsync(CancellationToken ct = default)
        => await db.PaymentMethods.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.DisplayOrder).ToArrayAsync(ct);

    public Task<PaymentMethod?> FindActiveMethodAsync(int id, CancellationToken ct = default)
        => db.PaymentMethods.SingleOrDefaultAsync(item => item.Id == id && item.IsActive, ct);

    public Task<Student?> FindStudentByDniAsync(string dni, CancellationToken ct = default)
        => db.Students.Include(item => item.User).SingleOrDefaultAsync(item => item.User.Dni == dni, ct);

    public async Task<IReadOnlyList<StudentDebt>> GetDebtsByPublicIdsAsync(IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
        => await db.StudentDebts.Where(item => publicIds.Contains(item.PublicId)).OrderBy(item => item.Id).ToArrayAsync(ct);

    public void AddPayment(Payment payment) => db.Payments.Add(payment);

    public Task<Payment?> FindByConfirmationKeyForUpdateAsync(string idempotencyKey, CancellationToken ct = default)
        => Graph(db.Payments
            .FromSqlInterpolated($"SELECT * FROM \"Payments\" WHERE confirmation_idempotency_key = {idempotencyKey} FOR UPDATE"))
            .SingleOrDefaultAsync(ct);

    public Task<Payment?> FindForUpdateAsync(Guid publicId, CancellationToken ct = default)
        => Graph(db.Payments
            .FromSqlInterpolated($"SELECT * FROM \"Payments\" WHERE public_id = {publicId} FOR UPDATE"))
            .SingleOrDefaultAsync(ct);

    public async Task<IReadOnlyList<StudentDebt>> LockDebtsForPaymentAsync(long paymentId, CancellationToken ct = default)
    {
        var debts = await db.StudentDebts
            .FromSqlInterpolated($"SELECT d.* FROM \"StudentDebts\" d WHERE EXISTS (SELECT 1 FROM \"PaymentAllocations\" a WHERE a.payment_id = {paymentId} AND a.student_debt_id = d.\"Id\") FOR UPDATE OF d")
            .OrderBy(item => item.Id)
            .ToArrayAsync(ct);
        foreach (var debt in debts) await db.Entry(debt).ReloadAsync(ct);
        return debts;
    }

    public async Task<IReadOnlyList<Payment>> GetByStudentAsync(long studentId, CancellationToken ct = default)
        => await Graph(db.Payments.AsNoTracking().Where(item => item.StudentId == studentId))
            .OrderByDescending(item => item.CreatedAt).ToArrayAsync(ct);

    public async Task<IReadOnlyList<Payment>> GetByUserAsync(long userId, CancellationToken ct = default)
        => await Graph(db.Payments.AsNoTracking().Where(item => item.Student.UserId == userId))
            .OrderByDescending(item => item.CreatedAt).ToArrayAsync(ct);

    private static IQueryable<Payment> Graph(IQueryable<Payment> query)
        => query
            .Include(item => item.Student).ThenInclude(item => item.User)
            .Include(item => item.PaymentMethod)
            .Include(item => item.Allocations).ThenInclude(item => item.StudentDebt).ThenInclude(item => item.FinancialConcept)
            .Include(item => item.Reconciliations)
            .Include(item => item.Reversals)
            .Include(item => item.Receipt);
}
