namespace AcademiaDigital.Application.Interfaces;

public sealed record TranscriptStudent(
    long StudentId, string LastName, string FirstName, string? Dni, string? Cuil, string? Gender,
    DateOnly? BirthDate, string? AddressLine, string? City, string? Province, string? Phone, string Email,
    string LegajoNumber, string? RegistryBook, string? RegistryFolio, string Status, int? AdmissionYear,
    DateOnly? GraduatedOn, string CareerName, string? StudyPlanName);

/// <summary>Una materia del plan con cómo se aprobó y dónde quedó registrada (libro y folio del acta).</summary>
public sealed record TranscriptCourse(
    int CourseId, string CourseCode, string CourseName, int YearNumber, string Period,
    string Condition, decimal? FinalGrade, DateOnly? ApprovedOn, long? ExamTableId, string? ActBook, string? ActFolio);

public sealed record TranscriptDto(TranscriptStudent Student, IReadOnlyList<TranscriptCourse> Courses, int ApprovedCount, decimal? Average);

/// <summary>Analítico: datos personales y académicos del alumno con todas las materias del plan.</summary>
public interface ITranscriptService
{
    Task<TranscriptDto> GetAsync(long studentId, CancellationToken ct = default);
    Task SetRegistryAsync(long studentId, string? book, string? folio, CancellationToken ct = default);
    Task SetExamActAsync(long examTableId, string? book, string? folio, CancellationToken ct = default);
}
