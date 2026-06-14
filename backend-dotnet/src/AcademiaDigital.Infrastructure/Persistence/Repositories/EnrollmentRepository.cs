using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public class EnrollmentRepository(AppDbContext db) : IEnrollmentRepository
{
    public async Task<IEnumerable<Enrollment>> GetByStudentAsync(long studentId, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Course)
            .Include(e => e.TeachingPosition).ThenInclude(tp => tp!.Teacher).ThenInclude(t => t!.User)
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.AcademicYear).ThenByDescending(e => e.Semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<Enrollment>> GetByCourseAndPeriodAsync(int courseId, int year, int semester, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Where(e => e.CourseId == courseId && e.AcademicYear == year && e.Semester == semester)
            .ToListAsync(ct);

    public async Task<IEnumerable<Enrollment>> GetByTeachingPositionAsync(int teachingPositionId, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.Course)
            .Where(e => e.TeachingPositionId == teachingPositionId)
            .ToListAsync(ct);

    public async Task<Enrollment?> FindByIdAsync(long id, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Include(e => e.TeachingPosition)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Enrollment?> FindByStudentAndCourseAsync(long studentId, int courseId, int year, int semester, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId
                && e.AcademicYear == year && e.Semester == semester, ct);

    public async Task<Enrollment> CreateAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);
        return enrollment;
    }

    public async Task<Enrollment> UpdateAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        db.Enrollments.Update(enrollment);
        await db.SaveChangesAsync(ct);
        return enrollment;
    }

    public async Task DeleteAsync(Enrollment enrollment, CancellationToken ct = default)
    {
        db.Enrollments.Remove(enrollment);
        await db.SaveChangesAsync(ct);
    }
}
