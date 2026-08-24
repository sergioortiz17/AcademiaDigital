using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICertificateRequestRepository
{
    Task<List<CertificateRequest>> GetByUserAsync(long userId, CancellationToken ct = default);
    Task<List<CertificateRequest>> GetAllAsync(string? search, CertificateStatus? status, CancellationToken ct = default);
    Task<CertificateRequest> CreateAsync(CertificateRequest request, CancellationToken ct = default);
    Task<CertificateRequest?> FindByIdAsync(long id, CancellationToken ct = default);
    Task<CertificateRequest?> FindForUpdateAsync(long id, CancellationToken ct = default);
    Task<bool> HasActiveRequestAsync(long userId, long studentCareerId, CertificateKind kind, long? examRegistrationId, CancellationToken ct = default);
    Task<CertificateAcademicRecord?> GetAcademicRecordAsync(long userId, long? studentCareerId, long? examRegistrationId, CancellationToken ct = default);
    Task<CertificateSequence> LockSequenceAsync(CancellationToken ct = default);
    Task<CertificateIssuance?> FindIssuanceByRequestAsync(long requestId, bool tracking, CancellationToken ct = default);
    Task<CertificateIssuance?> FindIssuanceByPublicIdAsync(Guid publicId, CancellationToken ct = default);
    Task<IReadOnlyList<CertificateIssuance>> GetHistoryByUserAsync(long userId, CancellationToken ct = default);
    Task<IReadOnlyList<CertificateIssuance>> GetHistoryByStudentAsync(long studentId, CancellationToken ct = default);
    void AddIssuance(CertificateIssuance issuance);
}

public sealed record CertificateAcademicRecord(
    long StudentId,
    long StudentCareerId,
    bool StudentCareerIsActive,
    StudentStatus StudentStatus,
    string LegajoNumber,
    string Dni,
    string StudentName,
    string CareerName,
    IReadOnlyList<CertificateCourseRecord> Courses,
    CertificateExamRecord? Exam);

public sealed record CertificateCourseRecord(
    int CourseId,
    string Code,
    string Name,
    int AcademicYear,
    int Semester,
    EnrollmentStatus Status,
    decimal? FinalGrade);

public sealed record CertificateExamRecord(
    long RegistrationId,
    string CourseCode,
    string CourseName,
    DateTime ExamDateUtc,
    string Location,
    int CallNumber,
    ExamTableStatus Status);
