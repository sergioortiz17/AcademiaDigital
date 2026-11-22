using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class CourseSectionRepository(AppDbContext db) : ICourseSectionRepository
{
    public async Task<IReadOnlyList<CourseSection>> GetAllAsync(
        int? academicYear,
        int? semester,
        bool? isVacant,
        bool includeInactive,
        CancellationToken ct = default)
    {
        var query = Details().Where(position => includeInactive || position.IsActive);
        if (academicYear.HasValue) query = query.Where(position => position.AcademicYear == academicYear);
        if (semester.HasValue) query = query.Where(position => position.Semester == semester);
        if (isVacant.HasValue) query = query.Where(position => position.IsVacant == isVacant);
        return await query.OrderByDescending(position => position.AcademicYear)
            .ThenBy(position => position.Semester)
            .ThenBy(position => position.Course.Code)
            .ToArrayAsync(ct);
    }

    public async Task<IEnumerable<CourseSection>> GetByCourseAsync(int courseId, CancellationToken ct = default)
        => await db.CourseSections.AsNoTracking()
            .Include(tp => tp.Teacher).ThenInclude(t => t!.User)
            .Where(tp => tp.CourseId == courseId)
            .OrderBy(tp => tp.AcademicYear).ThenBy(tp => tp.Semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<CourseSection>> GetByTeacherAsync(long teacherId, CancellationToken ct = default)
        => await db.CourseSections.AsNoTracking()
            .Include(tp => tp.Course)
            .Where(tp => tp.TeacherId == teacherId)
            .OrderByDescending(tp => tp.AcademicYear).ThenByDescending(tp => tp.Semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<CourseSection>> GetByPeriodAsync(int year, int semester, CancellationToken ct = default)
        => await db.CourseSections.AsNoTracking()
            .Include(tp => tp.Course)
            .Include(tp => tp.Teacher).ThenInclude(t => t!.User)
            .Where(tp => tp.AcademicYear == year && tp.Semester == semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<CourseSection>> GetVacantAsync(CancellationToken ct = default)
        => await db.CourseSections.AsNoTracking()
            .Include(tp => tp.Course)
            .Where(tp => tp.IsVacant)
            .ToListAsync(ct);

    public async Task<CourseSection?> FindByIdAsync(int id, CancellationToken ct = default)
        => await Details()
            .FirstOrDefaultAsync(tp => tp.Id == id, ct);

    public async Task<IReadOnlyList<CourseSection>> FindActiveByCourseTermAsync(
        int courseId, int academicYear, int semester, bool isAnnual, CancellationToken ct = default)
        => await db.CourseSections.AsNoTracking()
            .Where(section => section.IsActive
                && section.CourseId == courseId
                && section.AcademicYear == academicYear
                && (isAnnual ? section.IsAnnual : (!section.IsAnnual && section.Semester == semester)))
            .ToListAsync(ct);

    public async Task<CourseSection> CreateAsync(CourseSection position, CancellationToken ct = default)
    {
        db.CourseSections.Add(position);
        await db.SaveChangesAsync(ct);
        return position;
    }

    public async Task<CourseSection> UpdateAsync(CourseSection position, CancellationToken ct = default)
    {
        db.Entry(position).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return position;
    }

    public async Task DeactivateAsync(CourseSection position, CancellationToken ct = default)
    {
        db.Entry(position).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<CourseSection> Details()
        => db.CourseSections.AsNoTracking()
            .Include(position => position.Course)
            .Include(position => position.Division)
            .Include(position => position.Teacher).ThenInclude(teacher => teacher!.User);

    public async Task<IReadOnlyDictionary<int, int>> CountEnrolledAsync(
        IReadOnlyCollection<CourseSection> sections, CancellationToken ct = default)
    {
        if (sections.Count == 0) return new Dictionary<int, int>();
        var courseIds = sections.Select(s => s.CourseId).Distinct().ToArray();
        var years = sections.Select(s => s.AcademicYear).Distinct().ToArray();
        // Una sola consulta por tabla y el cruce en memoria (las cantidades son chicas).
        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId) && years.Contains(e.AcademicYear)
                && e.Status != EnrollmentStatus.Withdrawn)
            .Select(e => new { e.CourseId, e.AcademicYear, e.Semester, e.CourseSectionId, e.StudentCareerId })
            .ToListAsync(ct);
        var assignments = (await db.StudentAcademicAssignments.AsNoTracking()
                .Where(a => years.Contains(a.AcademicYear) && a.DivisionId != null)
                .Select(a => new { a.StudentCareerId, a.DivisionId, a.AcademicYear })
                .ToListAsync(ct))
            .Select(a => (a.StudentCareerId, a.DivisionId, a.AcademicYear))
            .ToHashSet();
        return sections.ToDictionary(section => section.Id, section => enrollments.Count(e =>
            e.CourseId == section.CourseId && e.AcademicYear == section.AcademicYear && e.Semester == section.Semester
            && (e.CourseSectionId == section.Id
                || (e.CourseSectionId == null
                    && assignments.Contains((e.StudentCareerId, section.DivisionId, section.AcademicYear))))));
    }
}
