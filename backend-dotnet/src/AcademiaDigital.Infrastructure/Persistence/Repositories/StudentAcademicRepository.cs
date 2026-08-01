using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class StudentAcademicRepository(AppDbContext db) : IStudentAcademicRepository
{
    public async Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, CancellationToken ct = default)
        => await db.StudentStudyPlans.AsNoTracking()
            .Include(ssp => ssp.Student).ThenInclude(s => s.Career)
            .Include(ssp => ssp.StudentCareer)
            .Include(ssp => ssp.StudyPlan).ThenInclude(sp => sp.Career)
            .FirstOrDefaultAsync(ssp => ssp.StudentId == studentId && ssp.IsCurrent &&
                ssp.StudentCareer.CareerId == ssp.Student.CareerId, ct);

    public async Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, int careerId, CancellationToken ct = default)
        => await db.StudentStudyPlans.AsNoTracking()
            .Include(ssp => ssp.StudentCareer).ThenInclude(sc => sc.Career)
            .Include(ssp => ssp.StudyPlan).ThenInclude(sp => sp.Career)
            .FirstOrDefaultAsync(ssp => ssp.StudentId == studentId && ssp.IsCurrent &&
                ssp.StudentCareer.CareerId == careerId, ct);

    public async Task<IReadOnlyDictionary<long, StudentStudyPlan>> GetCurrentStudyPlansAsync(IEnumerable<long> studentIds, CancellationToken ct = default)
    {
        var ids = studentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, StudentStudyPlan>();

        var currentPlans = await db.StudentStudyPlans.AsNoTracking()
            .Include(ssp => ssp.StudyPlan)
            .Include(ssp => ssp.StudentCareer)
            .Include(ssp => ssp.Student)
            .Where(ssp => ids.Contains(ssp.StudentId) && ssp.IsCurrent &&
                ssp.StudentCareer.CareerId == ssp.Student.CareerId)
            .ToListAsync(ct);

        return currentPlans.ToDictionary(ssp => ssp.StudentId);
    }

    public async Task<IReadOnlyDictionary<int, StudentStudyPlan>> GetCurrentStudyPlansByCareerAsync(long studentId, CancellationToken ct = default)
        => await db.StudentStudyPlans.AsNoTracking().Include(x => x.StudyPlan).Include(x => x.StudentCareer)
            .Where(x => x.StudentId == studentId && x.IsCurrent)
            .ToDictionaryAsync(x => x.StudentCareer.CareerId, ct);

    public async Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, int careerId, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId && e.StudentCareer.CareerId == careerId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanCourse>> GetStudyPlanCoursesAsync(int studyPlanId, CancellationToken ct = default)
        => await db.StudyPlanCourses.AsNoTracking()
            .Include(spc => spc.Course)
            .Include(spc => spc.CourseType)
            .Where(spc => spc.StudyPlanId == studyPlanId && spc.IsActive)
            .OrderBy(spc => spc.YearNumber)
            .ThenBy(spc => spc.Semester)
            .ThenBy(spc => spc.SortOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CoursePrerequisite>> GetPrerequisitesAsync(int studyPlanId, CancellationToken ct = default)
        => await db.CoursePrerequisites.AsNoTracking()
            .Include(cp => cp.PrerequisiteCourse)
            .Where(cp => cp.StudyPlanId == studyPlanId && cp.IsActive)
            .ToListAsync(ct);

    public async Task<StudentStudyPlan> AssignStudyPlanAsync(StudentStudyPlan studentStudyPlan, CancellationToken ct = default)
    {
        var currentAssignments = await db.StudentStudyPlans
            .Where(ssp => ssp.StudentCareerId == studentStudyPlan.StudentCareerId && ssp.IsCurrent)
            .ToListAsync(ct);

        foreach (var currentAssignment in currentAssignments)
        {
            currentAssignment.IsCurrent = false;
            currentAssignment.EndedAt = DateTime.UtcNow;
        }

        studentStudyPlan.IsCurrent = true;
        studentStudyPlan.AssignedAt = DateTime.UtcNow;
        db.StudentStudyPlans.Add(studentStudyPlan);
        await db.SaveChangesAsync(ct);
        return studentStudyPlan;
    }
}
