using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Certificates;

public class CreateCertificateRequestUseCase(
    ICertificateRequestRepository repository,
    CertificatePolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<CertificateRequestDto> ExecuteAsync(
        long userId,
        string certificateType,
        long? studentCareerId = null,
        long? examRegistrationId = null,
        CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var kind = policy.ParseKind(certificateType);
            var academic = await repository.GetAcademicRecordAsync(
                userId, studentCareerId, examRegistrationId, transactionCt)
                ?? throw new KeyNotFoundException("Carrera del alumno no encontrada.");
            policy.EnsureEligible(kind, academic, examRegistrationId);
            if (await repository.HasActiveRequestAsync(
                    userId, academic.StudentCareerId, kind, examRegistrationId, transactionCt))
                throw new InvalidOperationException("Ya existe una solicitud activa para este certificado.");

            var request = await repository.CreateAsync(new CertificateRequest
            {
                UserId = userId,
                CertificateType = policy.DisplayName(kind),
                Kind = kind,
                StudentCareerId = academic.StudentCareerId,
                ExamRegistrationId = examRegistrationId,
                Status = CertificateStatus.Pending,
                CreatedAt = timeProvider.GetUtcNow().UtcDateTime
            }, transactionCt);
            return GetCertificateRequestsUseCase.Map(request);
        }, ct);
}
