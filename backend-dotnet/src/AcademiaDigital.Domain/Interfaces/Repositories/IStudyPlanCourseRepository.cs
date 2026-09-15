using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface IStudyPlanCourseRepository
{
    Task<StudyPlanCourse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanCourse>> GetByStudyPlanIdAsync(int studyPlanId, CancellationToken ct = default);
    Task<bool> ExistsAsync(int studyPlanId, int courseId, CancellationToken ct = default);
    Task<StudyPlanCourse> CreateAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default);
    Task<StudyPlanCourse> UpdateAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default);
    Task DeleteAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default);
    Task DeleteByStudyPlanIdsAsync(IReadOnlyList<int> studyPlanIds, CancellationToken ct = default);

    /// <summary>Upsert de la CourseApprovalRule de una materia del plan (crea si no existe, si no edita).</summary>
    Task SetApprovalRuleAsync(
        int studyPlanId, int studyPlanCourseId,
        decimal? minimumRegularGrade, decimal? minimumPromotionGrade, decimal minimumFinalExamGrade,
        decimal? minimumAttendancePercentage, bool requiresFinalExam, bool allowsPromotion,
        CancellationToken ct = default);
}
