using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class StudentAcademicRepository(AppDbContext db) : IStudentAcademicRepository
{
    public async Task<StudentStudyPlan?> GetCurrentStudyPlanAsync(long studentId, CancellationToken ct = default)
        => await db.StudentStudyPlans.AsNoTracking()
            .Include(ssp => ssp.Student).ThenInclude(s => s.Career)
            .Include(ssp => ssp.StudyPlan).ThenInclude(sp => sp.Career)
            .FirstOrDefaultAsync(ssp => ssp.StudentId == studentId && ssp.IsCurrent, ct);

    public async Task<IReadOnlyList<Enrollment>> GetEnrollmentsAsync(long studentId, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
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
        db.StudentStudyPlans.Add(studentStudyPlan);
        await db.SaveChangesAsync(ct);
        return studentStudyPlan;
    }
}
