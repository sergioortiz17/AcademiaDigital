using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public sealed record GradebookRosterRow(
    long EnrollmentId,
    long StudentId,
    string StudentName,
    string LegajoNumber,
    string Dni);

public sealed record EnrollmentGradebookResult(long EnrollmentId, decimal Average, EnrollmentStatus Status);

public interface IGradebookRepository
{
    Task<bool> CanTeacherManagePositionAsync(long userId, int teachingPositionId, CancellationToken ct = default);
    Task<bool> CanTeacherManageGradebookAsync(long userId, long gradebookId, CancellationToken ct = default);
    Task<IReadOnlyList<Gradebook>> GetGradebooksAsync(
        int? academicYear,
        int? courseId,
        int? commissionId,
        long? teacherUserId,
        CancellationToken ct = default);
    Task<Gradebook?> FindAsync(long gradebookId, CancellationToken ct = default);
    Task<Gradebook?> FindForUpdateAsync(long gradebookId, CancellationToken ct = default);
    Task<(Gradebook Gradebook, bool Created)> CreateIdempotentAsync(Gradebook gradebook, CancellationToken ct = default);
    Task<IReadOnlyList<GradebookRosterRow>> GetRosterAsync(Gradebook gradebook, CancellationToken ct = default);
    Task SaveGradeRevisionsAsync(IReadOnlyList<GradeEntryRevision> revisions, CancellationToken ct = default);
    Task SaveAsync(Gradebook gradebook, CancellationToken ct = default);
    Task ApplyFinalResultsAsync(Gradebook gradebook, IReadOnlyList<EnrollmentGradebookResult> results, CancellationToken ct = default);
    Task<IReadOnlyList<Gradebook>> GetPublishedForStudentAsync(long studentId, int? courseId, CancellationToken ct = default);
}
