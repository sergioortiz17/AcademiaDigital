using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<IReadOnlyList<PaymentMethod>> GetActiveMethodsAsync(CancellationToken ct = default);
    Task<PaymentMethod?> FindActiveMethodAsync(int id, CancellationToken ct = default);
    Task<Student?> FindStudentByDniAsync(string dni, CancellationToken ct = default);
    Task<IReadOnlyList<StudentDebt>> GetDebtsByPublicIdsAsync(IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default);
    void AddPayment(Payment payment);

    Task<Payment?> FindByConfirmationKeyForUpdateAsync(string idempotencyKey, CancellationToken ct = default);
    Task<Payment?> FindForUpdateAsync(Guid publicId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentDebt>> LockDebtsForPaymentAsync(long paymentId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByStudentAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByUserAsync(long userId, CancellationToken ct = default);
}
