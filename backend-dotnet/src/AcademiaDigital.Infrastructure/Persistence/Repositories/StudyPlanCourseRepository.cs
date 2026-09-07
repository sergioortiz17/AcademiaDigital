using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class StudyPlanCourseRepository(AppDbContext db) : IStudyPlanCourseRepository
{
    public async Task<StudyPlanCourse?> GetByIdAsync(int id, CancellationToken ct = default)
        => await db.StudyPlanCourses.AsNoTracking()
            .Include(spc => spc.StudyPlan)
            .Include(spc => spc.Course)
            .Include(spc => spc.CourseType)
            .Include(spc => spc.ApprovalRule)
            .FirstOrDefaultAsync(spc => spc.Id == id, ct);

    public async Task<IReadOnlyList<StudyPlanCourse>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
        => await db.StudyPlanCourses.AsNoTracking()
            .Include(spc => spc.Course)
            .Where(spc => ids.Contains(spc.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanCourse>> GetByStudyPlanIdAsync(int studyPlanId, CancellationToken ct = default)
        => await db.StudyPlanCourses.AsNoTracking()
            .Include(spc => spc.Course)
            .Include(spc => spc.CourseType)
            .Include(spc => spc.ApprovalRule)
            .Where(spc => spc.StudyPlanId == studyPlanId && spc.IsActive)
            .OrderBy(spc => spc.YearNumber)
            .ThenBy(spc => spc.Semester)
            .ThenBy(spc => spc.SortOrder)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(int studyPlanId, int courseId, CancellationToken ct = default)
        => await db.StudyPlanCourses.AsNoTracking()
            .AnyAsync(spc => spc.StudyPlanId == studyPlanId && spc.CourseId == courseId, ct);

    public async Task<StudyPlanCourse> CreateAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default)
    {
        db.StudyPlanCourses.Add(studyPlanCourse);
        await db.SaveChangesAsync(ct);
        return studyPlanCourse;
    }

    public async Task<StudyPlanCourse> UpdateAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default)
    {
        db.StudyPlanCourses.Update(studyPlanCourse);
        await db.SaveChangesAsync(ct);
        return studyPlanCourse;
    }

    public async Task DeleteAsync(StudyPlanCourse studyPlanCourse, CancellationToken ct = default)
    {
        db.StudyPlanCourses.Remove(studyPlanCourse);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteByStudyPlanIdsAsync(IReadOnlyList<int> studyPlanIds, CancellationToken ct = default)
        => await db.StudyPlanCourses.Where(spc => studyPlanIds.Contains(spc.StudyPlanId)).ExecuteDeleteAsync(ct);

    public async Task SetApprovalRuleAsync(
        int studyPlanId, int studyPlanCourseId,
        decimal? minimumRegularGrade, decimal? minimumPromotionGrade, decimal minimumFinalExamGrade,
        decimal? minimumAttendancePercentage, bool requiresFinalExam, bool allowsPromotion,
        CancellationToken ct = default)
    {
        var studyPlanCourse = await db.StudyPlanCourses
            .Include(spc => spc.ApprovalRule)
            .FirstOrDefaultAsync(spc => spc.Id == studyPlanCourseId, ct)
            ?? throw new KeyNotFoundException("Materia del plan no encontrada.");

        if (studyPlanCourse.StudyPlanId != studyPlanId)
            throw new KeyNotFoundException("Materia del plan no encontrada.");

        var now = DateTime.UtcNow;
        if (studyPlanCourse.ApprovalRule is null)
        {
            studyPlanCourse.ApprovalRule = new CourseApprovalRule
            {
                StudyPlanCourseId = studyPlanCourse.Id,
                MinimumRegularGrade = minimumRegularGrade,
                MinimumPromotionGrade = minimumPromotionGrade,
                MinimumFinalExamGrade = minimumFinalExamGrade,
                MinimumAttendancePercentage = minimumAttendancePercentage,
                RequiresFinalExam = requiresFinalExam,
                AllowsPromotion = allowsPromotion,
                CreatedAt = now,
                UpdatedAt = now
            };
        }
        else
        {
            var existing = studyPlanCourse.ApprovalRule;
            existing.MinimumRegularGrade = minimumRegularGrade;
            existing.MinimumPromotionGrade = minimumPromotionGrade;
            existing.MinimumFinalExamGrade = minimumFinalExamGrade;
            existing.MinimumAttendancePercentage = minimumAttendancePercentage;
            existing.RequiresFinalExam = requiresFinalExam;
            existing.AllowsPromotion = allowsPromotion;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
    }
}
