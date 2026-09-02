using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Certificates;

public class GetCertificateRequestsUseCase(ICertificateRequestRepository repository)
{
    public async Task<List<CertificateRequestDto>> ExecuteAsync(long userId, CancellationToken ct = default)
        => (await repository.GetByUserAsync(userId, ct)).Select(Map).ToList();

    public static CertificateRequestDto Map(CertificateRequest request) => new(
        request.Id,
        request.UserId,
        request.User?.Username,
        request.User?.Email,
        request.CertificateType,
        PublicStatus(request.Status),
        request.CreatedAt,
        request.UpdatedAt,
        request.Kind.ToString(),
        request.StudentCareerId,
        request.ExamRegistrationId,
        request.ReviewedAt,
        request.ReviewedByUserId,
        request.RejectionReason,
        request.Issuance is null ? null : CertificateMappings.Map(request.Issuance));

    private static string PublicStatus(CertificateStatus status)
        => status is CertificateStatus.Issuing or CertificateStatus.Issued
            ? CertificateStatus.Approved.ToString()
            : status.ToString();
}

public record CertificateRequestDto(
    long Id,
    long UserId,
    string? Username,
    string? Email,
    string CertificateType,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string Kind,
    long? StudentCareerId,
    long? ExamRegistrationId,
    DateTime? ReviewedAt,
    long? ReviewedByUserId,
    string? RejectionReason,
    CertificateIssuanceDto? Issuance);

public sealed record CertificateIssuanceDto(
    Guid Id,
    string CertificateNumber,
    string CertificateType,
    string Status,
    string FileName,
    string? Sha256,
    DateTime CreatedAt,
    DateTime? GeneratedAt,
    string? DownloadPath,
    string? LastError,
    long UserId,
    string? Username,
    string? Email);

internal static class CertificateMappings
{
    public static CertificateIssuanceDto Map(CertificateIssuance issuance) => new(
        issuance.PublicId,
        issuance.CertificateNumber,
        issuance.CertificateRequest?.CertificateType ?? string.Empty,
        issuance.Status.ToString(),
        issuance.FileName,
        issuance.Sha256,
        issuance.CreatedAt,
        issuance.GeneratedAt,
        issuance.Status == CertificateIssuanceStatus.Ready
            ? $"/api/v1/certificates/issued/{issuance.PublicId}/download"
            : null,
        issuance.LastError,
        issuance.CertificateRequest?.UserId ?? 0,
        issuance.CertificateRequest?.User?.Username,
        issuance.CertificateRequest?.User?.Email);
}
