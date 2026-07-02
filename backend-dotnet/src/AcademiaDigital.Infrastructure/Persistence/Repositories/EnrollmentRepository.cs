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

    public async Task<IEnumerable<Enrollment>> GetByEnrollmentPeriodAsync(int periodId, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Include(e => e.Student).ThenInclude(s => s.User)
            .Include(e => e.Course)
            .Where(e => e.EnrollmentPeriodId == periodId)
            .OrderBy(e => e.Student.User.LastName)
            .ToListAsync(ct);

    // Projects only the columns we need — avoids loading full entity graphs.
    // Returns one row per (student, course); caller groups by StudentId.
    public async Task<IReadOnlyList<EnrollmentStudentRow>> GetStudentRowsByPeriodAsync(int periodId, CancellationToken ct = default)
    {
        var rows = await db.Enrollments
            .AsNoTracking()
            .Where(e => e.EnrollmentPeriodId == periodId)
            .OrderBy(e => e.Student.User.LastName)
            .ThenBy(e => e.Student.User.Username)
            .Select(e => new EnrollmentStudentRow(
                e.StudentId,
                e.Student.User.Username + " " + e.Student.User.LastName,
                e.Student.User.Dni ?? "",
                e.Shift,
                e.EnrollmentDate,
                e.Course.Name))
            .ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<MyEnrollmentRow>> GetMyEnrollmentRowsAsync(long studentId, CancellationToken ct = default)
        => await db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.AcademicYear)
            .ThenByDescending(e => e.Semester)
            .Select(e => new MyEnrollmentRow(
                e.EnrollmentPeriodId ?? 0,
                e.AcademicYear,
                e.Semester,
                e.Shift,
                e.EnrollmentDate,
                e.Course.Name))
            .ToListAsync(ct);

    public async Task DeleteByStudentAndPeriodAsync(long studentId, int periodId, CancellationToken ct = default)
    {
        var enrollments = await db.Enrollments
            .Where(e => e.StudentId == studentId && e.EnrollmentPeriodId == periodId)
            .ToListAsync(ct);

        if (enrollments.Count == 0) return;

        db.Enrollments.RemoveRange(enrollments);
        await db.SaveChangesAsync(ct);
    }

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
