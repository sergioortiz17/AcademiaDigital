using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IExamTableRepository
{
    Task<bool> CanTeacherManageAsync(long userId, long examTableId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamTable>> GetAsync(int? academicYear, int? courseId, long? teacherUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamTable>> GetForStudentAsync(long studentId, CancellationToken ct = default);
    Task<ExamTable?> FindAsync(long examTableId, CancellationToken ct = default);
    Task<ExamTable?> FindForUpdateAsync(long examTableId, CancellationToken ct = default);
    Task<(ExamTable ExamTable, bool Created)> CreateIdempotentAsync(ExamTable examTable, CancellationToken ct = default);
    Task<Enrollment?> FindEnrollmentForUpdateAsync(long enrollmentId, CancellationToken ct = default);
    Task<ExamRegistration> RegisterAsync(ExamTable examTable, Enrollment enrollment, long actorUserId, DateTime nowUtc, CancellationToken ct = default);
    Task SaveGradeRevisionsAsync(IReadOnlyList<ExamGradeRevision> revisions, CancellationToken ct = default);
    Task SaveAsync(ExamTable examTable, CancellationToken ct = default);
    Task PublishAsync(ExamTable examTable, CancellationToken ct = default);
}
