using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudentAcademicRepository
{
    Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetStudyPlanCoursesAsync(int studyPlanId, CancellationToken ct = default);
    Task<IReadOnlyList<CoursePrerequisite>> GetPrerequisitesAsync(int studyPlanId, CancellationToken ct = default);
    Task<StudentStudyPlan> AssignStudyPlanAsync(StudentStudyPlan studentStudyPlan, CancellationToken ct = default);
}
