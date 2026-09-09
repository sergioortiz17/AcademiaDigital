using AcademiaDigital.Finance.Domain.Entities;

namespace AcademiaDigital.Finance.Domain.Interfaces.Repositories;

public interface IReceiptRepository
{
    Task<ReceiptSequence> LockSequenceAsync(CancellationToken ct = default);
    Task<Receipt?> FindByPaymentAsync(long paymentId, bool tracking, CancellationToken ct = default);
    Task<Receipt?> FindByPublicIdAsync(Guid publicId, bool tracking, CancellationToken ct = default);
    Task<IReadOnlyList<Receipt>> GetByStudentAsync(long studentId, CancellationToken ct = default);
    void Add(Receipt receipt);
}
