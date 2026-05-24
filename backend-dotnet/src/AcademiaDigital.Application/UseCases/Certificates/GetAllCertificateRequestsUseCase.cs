using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Certificates;

public class GetAllCertificateRequestsUseCase(ICertificateRequestRepository repository)
{
    public async Task<List<CertificateRequestDto>> ExecuteAsync(CancellationToken ct = default)
    {
        var requests = await repository.GetAllAsync(ct);
        return requests.Select(r => new CertificateRequestDto(
            r.Id, r.UserId, r.CertificateType, r.Status.ToString(), r.CreatedAt, r.UpdatedAt
        )).ToList();
    }
}
