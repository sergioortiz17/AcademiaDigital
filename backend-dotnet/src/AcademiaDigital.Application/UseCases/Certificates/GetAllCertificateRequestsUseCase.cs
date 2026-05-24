using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Certificates;

public class GetAllCertificateRequestsUseCase(ICertificateRequestRepository repository)
{
    public async Task<List<CertificateRequestDto>> ExecuteAsync(string? search, CertificateStatus? status, CancellationToken ct = default)
    {
        var requests = await repository.GetAllAsync(search, status, ct);
        return requests.Select(r => new CertificateRequestDto(
            r.Id, r.UserId, r.User.Username, r.User.Email,
            r.CertificateType, r.Status.ToString(), r.CreatedAt, r.UpdatedAt
        )).ToList();
    }
}
