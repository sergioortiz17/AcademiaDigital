using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Certificates;

public class CreateCertificateRequestUseCase(ICertificateRequestRepository repository)
{
    public async Task<CertificateRequestDto> ExecuteAsync(long userId, string certificateType, CancellationToken ct = default)
    {
        var request = await repository.CreateAsync(userId, certificateType, ct);
        return GetCertificateRequestsUseCase.Map(request);
    }
}
