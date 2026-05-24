using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICertificateRequestRepository
{
    Task<List<CertificateRequest>> GetByUserAsync(long userId, CancellationToken ct = default);
    Task<List<CertificateRequest>> GetAllAsync(string? search, CertificateStatus? status, CancellationToken ct = default);
    Task<CertificateRequest> CreateAsync(long userId, string certificateType, CancellationToken ct = default);
    Task<CertificateRequest?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<CertificateRequest> UpdateStatusAsync(long id, CertificateStatus status, CancellationToken ct = default);
}
