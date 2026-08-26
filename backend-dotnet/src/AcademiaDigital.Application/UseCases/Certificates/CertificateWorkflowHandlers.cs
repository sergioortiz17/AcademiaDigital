using System.Security.Cryptography;
using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Certificates;

public sealed record ReviewCertificateRequestCommand(long RequestId, bool Approve, string? Reason, long ActorUserId);

public sealed class ReviewCertificateRequestCommandHandler(
    ICertificateRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public Task<CertificateRequestDto> Handle(ReviewCertificateRequestCommand command, CancellationToken ct = default)
        => unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var request = await repository.FindForUpdateAsync(command.RequestId, transactionCt)
                ?? throw new KeyNotFoundException("Certificate request not found.");
            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (command.Approve) request.Approve(command.ActorUserId, now);
            else request.Reject(command.ActorUserId, command.Reason ?? string.Empty, now);
            await unitOfWork.SaveChangesAsync(transactionCt);
            return GetCertificateRequestsUseCase.Map(request);
        }, ct);
}

public sealed record IssueCertificateCommand(long RequestId, long ActorUserId);

public sealed record CertificateSnapshot(
    string CertificateType,
    string StudentName,
    string Dni,
    string LegajoNumber,
    string CareerName,
    DateTime IssuedAt,
    string IssuerName,
    IReadOnlyList<CertificateCourseSnapshot> Courses,
    CertificateExamSnapshot? Exam);

public sealed record CertificateCourseSnapshot(
    string Code,
    string Name,
    int AcademicYear,
    int Semester,
    string Status,
    decimal? FinalGrade);

public sealed record CertificateExamSnapshot(
    string CourseCode,
    string CourseName,
    DateTime ExamDateUtc,
    string Location,
    int CallNumber);

public sealed class IssueCertificateCommandHandler(
    ICertificateRequestRepository repository,
    CertificatePolicy policy,
    IUnitOfWork unitOfWork,
    ICertificatePdfGenerator pdfGenerator,
    IFileStorage fileStorage,
    TimeProvider timeProvider)
{
    public async Task<CertificateIssuanceDto> Handle(IssueCertificateCommand command, CancellationToken ct = default)
    {
        var prepared = await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            // The singleton is the global lock-order root for every issuance. Taking it
            // before request/academic rows prevents inverted locks between concurrent
            // certificates while keeping the correlativo and its ledger row atomic.
            var sequence = await repository.LockSequenceAsync(transactionCt);
            var request = await repository.FindForUpdateAsync(command.RequestId, transactionCt)
                ?? throw new KeyNotFoundException("Certificate request not found.");
            var existing = await repository.FindIssuanceByRequestAsync(request.Id, true, transactionCt);
            if (existing is not null)
                return (Request: request, Issuance: existing);

            var academic = await repository.GetAcademicRecordAsync(
                request.UserId, request.StudentCareerId, request.ExamRegistrationId, transactionCt)
                ?? throw new KeyNotFoundException("Student career not found.");
            policy.EnsureEligible(request.Kind, academic, request.ExamRegistrationId);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            request.MarkIssuing(now);
            var number = sequence.TakeNext();
            var certificateNumber = $"CERT-{number:00000000}";
            var issuer = $"Usuario {command.ActorUserId}";
            var snapshot = BuildSnapshot(request.Kind, request.CertificateType, academic, now, issuer);
            var issuance = new CertificateIssuance
            {
                PublicId = Guid.NewGuid(),
                CertificateRequestId = request.Id,
                CertificateRequest = request,
                SequenceNumber = number,
                CertificateNumber = certificateNumber,
                SnapshotJson = JsonSerializer.Serialize(snapshot),
                Status = CertificateIssuanceStatus.Generating,
                FileName = $"{certificateNumber}.pdf",
                ContentType = "application/pdf",
                CreatedAt = now,
                IssuedByUserId = command.ActorUserId
            };
            repository.AddIssuance(issuance);
            await unitOfWork.SaveChangesAsync(transactionCt);
            return (Request: request, Issuance: issuance);
        }, ct);

        if (prepared.Issuance.Status == CertificateIssuanceStatus.Ready)
            return CertificateMappings.Map(prepared.Issuance);

        try
        {
            var snapshot = JsonSerializer.Deserialize<CertificateSnapshot>(prepared.Issuance.SnapshotJson)
                ?? throw new InvalidOperationException("Certificate snapshot is invalid.");
            var pdf = await pdfGenerator.GenerateAsync(ToPdfModel(prepared.Issuance.CertificateNumber, snapshot), ct);
            var storageKey = await fileStorage.SaveAsync(
                $"certificates/{prepared.Issuance.CreatedAt:yyyy}/{prepared.Issuance.PublicId:N}/{prepared.Issuance.FileName}",
                pdf, prepared.Issuance.ContentType, prepared.Issuance.FileName, ct);
            prepared.Issuance.MarkReady(storageKey, Convert.ToHexString(SHA256.HashData(pdf)), timeProvider.GetUtcNow().UtcDateTime);
            if (prepared.Request.Status == CertificateStatus.Issuing)
                prepared.Request.MarkIssued(prepared.Issuance.GeneratedAt!.Value);
            await unitOfWork.SaveChangesAsync(ct);
            return CertificateMappings.Map(prepared.Issuance);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            prepared.Issuance.MarkFailed(exception.Message);
            await unitOfWork.SaveChangesAsync(ct);
            throw new InvalidOperationException("Certificate generation failed and can be retried with the same number.", exception);
        }
    }

    private static CertificateSnapshot BuildSnapshot(
        CertificateKind kind,
        string certificateType,
        CertificateAcademicRecord academic,
        DateTime issuedAt,
        string issuerName)
    {
        var courses = kind switch
        {
            CertificateKind.RegularStudent or CertificateKind.ExamPermit => [],
            CertificateKind.Enrollment => academic.Courses
                .Where(course => course.Status != EnrollmentStatus.Withdrawn).ToArray(),
            CertificateKind.ApprovedCourses => academic.Courses
                .Where(course => course.Status is EnrollmentStatus.Approved or EnrollmentStatus.Promoted).ToArray(),
            _ => academic.Courses
        };
        return new(
            certificateType, academic.StudentName, academic.Dni, academic.LegajoNumber,
            academic.CareerName, issuedAt, issuerName,
            courses.Select(course => new CertificateCourseSnapshot(
                course.Code, course.Name, course.AcademicYear, course.Semester,
                course.Status.ToString(), course.FinalGrade)).ToArray(),
            academic.Exam is null ? null : new CertificateExamSnapshot(
                academic.Exam.CourseCode, academic.Exam.CourseName, academic.Exam.ExamDateUtc,
                academic.Exam.Location, academic.Exam.CallNumber));
    }

    private static CertificatePdfModel ToPdfModel(string certificateNumber, CertificateSnapshot snapshot)
        => new(
            certificateNumber, snapshot.CertificateType, snapshot.StudentName, snapshot.Dni,
            snapshot.LegajoNumber, snapshot.CareerName, snapshot.IssuedAt, snapshot.IssuerName,
            "Firma autorizada - Secretaría Académica", "Sello institucional - Academia Digital",
            snapshot.Courses.Select(course => new CertificatePdfCourse(
                course.Code, course.Name, course.AcademicYear, course.Semester,
                course.Status, course.FinalGrade)).ToArray(),
            snapshot.Exam is null ? null : new CertificatePdfExam(
                snapshot.Exam.CourseCode, snapshot.Exam.CourseName, snapshot.Exam.ExamDateUtc,
                snapshot.Exam.Location, snapshot.Exam.CallNumber));
}

public sealed record GetCertificateHistoryQuery(long ActorUserId, bool IsAdmin, long? StudentId = null);

public sealed class GetCertificateHistoryQueryHandler(ICertificateRequestRepository repository)
{
    public async Task<IReadOnlyList<CertificateIssuanceDto>> Handle(GetCertificateHistoryQuery query, CancellationToken ct = default)
    {
        if (query.StudentId.HasValue && !query.IsAdmin)
            throw new ForbiddenException("Only administrators can query another student's certificate history.");
        var items = query.StudentId.HasValue
            ? await repository.GetHistoryByStudentAsync(query.StudentId.Value, ct)
            : await repository.GetHistoryByUserAsync(query.ActorUserId, ct);
        return items.Select(CertificateMappings.Map).ToArray();
    }
}

public sealed record DownloadCertificateQuery(Guid PublicId, long ActorUserId, bool IsAdmin);

public sealed class DownloadCertificateQueryHandler(
    ICertificateRequestRepository repository,
    IFileStorage fileStorage)
{
    public async Task<StoredFile> Handle(DownloadCertificateQuery query, CancellationToken ct = default)
    {
        var issuance = await repository.FindIssuanceByPublicIdAsync(query.PublicId, ct)
            ?? throw new KeyNotFoundException("Issued certificate not found.");
        if (!query.IsAdmin && issuance.CertificateRequest.UserId != query.ActorUserId)
            throw new ForbiddenException("The certificate belongs to another student.");
        if (issuance.Status != CertificateIssuanceStatus.Ready || string.IsNullOrWhiteSpace(issuance.StorageKey))
            throw new InvalidOperationException("Certificate is not ready for download.");
        var stored = await fileStorage.ReadAsync(
            issuance.StorageKey, issuance.ContentType, issuance.FileName, ct)
            ?? throw new KeyNotFoundException("Certificate file not found.");
        var actualHash = Convert.ToHexString(SHA256.HashData(stored.Content));
        if (!string.Equals(actualHash, issuance.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Certificate file integrity validation failed.");
        return stored;
    }
}
