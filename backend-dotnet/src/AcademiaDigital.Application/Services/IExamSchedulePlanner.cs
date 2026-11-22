namespace AcademiaDigital.Application.Services;

public sealed record TribunalSuggestion(long TeacherId, string TeacherName, string Role);
public sealed record ExamProposalRow(
    int CourseId, string CourseCode, string CourseName, int YearNumber, DateOnly? Date, string? Reason,
    IReadOnlyList<TribunalSuggestion> Tribunal);

/// <summary>Propuesta de mesas de examen de una carrera para un rango de fechas (no crea nada).</summary>
public interface IExamSchedulePlanner
{
    Task<IReadOnlyList<ExamProposalRow>> ProposeAsync(
        int careerId, int academicYear, IReadOnlyList<DateOnly> availableDays, IReadOnlyCollection<int>? yearNumbers,
        CancellationToken ct = default);
}
