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
}
