namespace AcademiaDigital.Application.Interfaces;

public sealed record CertificatePdfCourse(
    string Code,
    string Name,
    int AcademicYear,
    int Semester,
    string Status,
    decimal? FinalGrade);

public sealed record CertificatePdfExam(
    string CourseCode,
    string CourseName,
    DateTime ExamDateUtc,
    string Location,
    int CallNumber);

public sealed record CertificatePdfModel(
    string CertificateNumber,
    string CertificateType,
    string StudentName,
    string Dni,
    string LegajoNumber,
    string CareerName,
    DateTime IssuedAt,
    string IssuerName,
    string SignatureText,
    string SealText,
    IReadOnlyList<CertificatePdfCourse> Courses,
    CertificatePdfExam? Exam,
    // Multitenant: null conserva el encabezado y logo históricos (ITSC).
    string? InstitutionName = null,
    bool UseDefaultLogo = true);

public interface ICertificatePdfGenerator
{
    Task<byte[]> GenerateAsync(CertificatePdfModel model, CancellationToken ct = default);
}
