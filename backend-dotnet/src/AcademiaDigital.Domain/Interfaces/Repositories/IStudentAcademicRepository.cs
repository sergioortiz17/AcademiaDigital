using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudentAcademicRepository
{
    Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, CancellationToken ct = default);
    Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, int careerId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<long, StudentStudyPlan>> GetCurrentStudyPlansAsync(IEnumerable<long> studentIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, StudentStudyPlan>> GetCurrentStudyPlansByCareerAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, int careerId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetStudyPlanCoursesAsync(int studyPlanId, CancellationToken ct = default);
    Task<IReadOnlyList<CoursePrerequisite>> GetPrerequisitesAsync(int studyPlanId, CancellationToken ct = default);
    Task<StudentStudyPlan> AssignStudyPlanAsync(StudentStudyPlan studentStudyPlan, CancellationToken ct = default);
}
