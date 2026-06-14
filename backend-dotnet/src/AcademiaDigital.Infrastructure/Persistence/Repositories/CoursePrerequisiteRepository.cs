using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CoursePrerequisiteRepository(AppDbContext db) : ICoursePrerequisiteRepository
{
    public async Task<IReadOnlyList<CoursePrerequisite>> GetByStudyPlanIdAsync(int studyPlanId, CancellationToken ct = default)
        => await db.CoursePrerequisites.AsNoTracking()
            .Include(cp => cp.Course)
            .Include(cp => cp.PrerequisiteCourse)
            .Where(cp => cp.StudyPlanId == studyPlanId && cp.IsActive)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CoursePrerequisite>> GetByCourseAsync(int studyPlanId, int courseId, CancellationToken ct = default)
        => await db.CoursePrerequisites.AsNoTracking()
            .Include(cp => cp.PrerequisiteCourse)
            .Where(cp => cp.StudyPlanId == studyPlanId && cp.CourseId == courseId && cp.IsActive)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(int studyPlanId, int courseId, int prerequisiteCourseId, CancellationToken ct = default)
        => await db.CoursePrerequisites.AsNoTracking()
            .AnyAsync(cp => cp.StudyPlanId == studyPlanId
                && cp.CourseId == courseId
                && cp.PrerequisiteCourseId == prerequisiteCourseId
                && cp.IsActive, ct);

    public async Task<CoursePrerequisite> CreateAsync(CoursePrerequisite prerequisite, CancellationToken ct = default)
    {
        db.CoursePrerequisites.Add(prerequisite);
        await db.SaveChangesAsync(ct);
        return prerequisite;
    }

    public async Task RemoveAsync(int studyPlanId, int courseId, int prerequisiteCourseId, CancellationToken ct = default)
    {
        await db.CoursePrerequisites
            .Where(cp => cp.StudyPlanId == studyPlanId
                && cp.CourseId == courseId
                && cp.PrerequisiteCourseId == prerequisiteCourseId)
            .ExecuteDeleteAsync(ct);
    }
}
