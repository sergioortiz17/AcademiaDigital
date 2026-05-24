using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Certificates;

public class GetCertificateRequestsUseCase(ICertificateRequestRepository repository)
{
    public async Task<List<CertificateRequestDto>> ExecuteAsync(long userId, CancellationToken ct = default)
    {
        var requests = await repository.GetByUserAsync(userId, ct);
        return requests.Select(Map).ToList();
    }

    public static CertificateRequestDto Map(CertificateRequest r) =>
        new(r.Id, r.UserId, null, null, r.CertificateType, r.Status.ToString(), r.CreatedAt, r.UpdatedAt);
}

public record CertificateRequestDto(
    long Id,
    long UserId,
    string? Username,
    string? Email,
    string CertificateType,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
