using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class BulkStudentActionsService(AppDbContext db) : IBulkStudentActionsService
{
    public const int MaxStudents = 500;

    public async Task<BulkGraduateResult> GraduateAsync(
        IReadOnlyCollection<long> studentIds, DateOnly graduatedOn, string reason, long actorUserId, CancellationToken ct = default)
    {
        var ids = studentIds.Distinct().ToArray();
        if (ids.Length == 0) throw new ArgumentException("Seleccione al menos un alumno.");
        if (ids.Length > MaxStudents) throw new ArgumentException($"Se pueden egresar hasta {MaxStudents} alumnos por vez.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Indique el motivo o acto administrativo.");
        if (graduatedOn > DateOnly.FromDateTime(DateTime.UtcNow)) throw new ArgumentException("La fecha de egreso no puede ser futura.");

        var students = await db.Students.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
        var failed = ids.Where(id => students.All(s => s.Id != id)).Select(id => new BulkFailure(id, "Alumno no encontrado.")).ToList();
        var now = DateTime.UtcNow;
        var graduated = 0;
        foreach (var student in students)
        {
            if (student.Status == StudentStatus.Graduated) { failed.Add(new(student.Id, "Ya figura como egresado.")); continue; }
            if (student.Status == StudentStatus.Withdrawn) { failed.Add(new(student.Id, "Está dado de baja.")); continue; }
            db.StudentStatusHistory.Add(new StudentStatusHistory
            {
                StudentId = student.Id, PreviousStatus = student.Status, NewStatus = StudentStatus.Graduated,
                Reason = reason.Trim(), ChangedByUserId = actorUserId, ChangedAt = now
            });
            student.Status = StudentStatus.Graduated;
            student.GraduatedOn = graduatedOn;
            student.UpdatedAt = now;
            graduated++;
        }
        await db.SaveChangesAsync(ct);
        return new BulkGraduateResult(graduated, failed);
    }

    public async Task<BulkEnrollResult> EnrollDivisionAsync(int divisionId, int semester, CancellationToken ct = default)
    {
        if (semester is not (1 or 2)) throw new ArgumentException("El cuatrimestre debe ser 1 o 2.");
        var division = await db.Divisions.AsNoTracking().SingleOrDefaultAsync(d => d.Id == divisionId, ct)
            ?? throw new KeyNotFoundException("División no encontrada.");

        var assignments = await db.StudentAcademicAssignments.AsNoTracking()
            .Where(a => a.DivisionId == divisionId && a.AcademicYear == division.AcademicYear && a.IsCurrent)
            .Select(a => new { a.StudentId, a.StudentCareerId, a.StudyPlanId })
            .ToListAsync(ct);
        if (assignments.Count == 0)
            throw new InvalidOperationException("La división no tiene alumnos asignados para su año académico.");

        var planIds = assignments.Select(a => a.StudyPlanId).Distinct().ToArray();
        var planCourses = await db.StudyPlanCourses.AsNoTracking()
            .Where(c => planIds.Contains(c.StudyPlanId) && c.IsActive && c.YearNumber == division.YearNumber
                && (semester == 1 ? (c.IsAnnual || c.Semester == 1) : (!c.IsAnnual && c.Semester == 2)))
            .Select(c => new { c.Id, c.StudyPlanId, c.CourseId, c.IsAnnual, c.Semester, CourseName = c.Course.Name })
            .ToListAsync(ct);
        if (planCourses.Count == 0)
            throw new InvalidOperationException("El plan no tiene materias de ese año y cuatrimestre.");

        var courseIds = planCourses.Select(c => c.CourseId).Distinct().ToArray();
        var sections = await db.CourseSections.AsNoTracking()
            .Where(s => s.DivisionId == divisionId && s.AcademicYear == division.AcademicYear && s.IsActive && courseIds.Contains(s.CourseId))
            .Select(s => new { s.Id, s.CourseId, s.Semester })
            .ToListAsync(ct);
        var studentIds = assignments.Select(a => a.StudentId).ToArray();
        var existing = (await db.Enrollments.AsNoTracking()
            .Where(e => studentIds.Contains(e.StudentId) && e.AcademicYear == division.AcademicYear && courseIds.Contains(e.CourseId))
            .Select(e => new { e.StudentId, e.CourseId, e.Semester })
            .ToListAsync(ct))
            .Select(e => (e.StudentId, e.CourseId, e.Semester)).ToHashSet();

        var created = 0;
        var already = 0;
        var now = DateTime.UtcNow;
        foreach (var assignment in assignments)
        {
            foreach (var course in planCourses.Where(c => c.StudyPlanId == assignment.StudyPlanId))
            {
                var enrollmentSemester = course.IsAnnual ? 1 : course.Semester;
                if (!existing.Add((assignment.StudentId, course.CourseId, enrollmentSemester))) { already++; continue; }
                db.Enrollments.Add(new Enrollment
                {
                    AcademicYear = division.AcademicYear,
                    Semester = enrollmentSemester,
                    EnrollmentDate = now,
                    Status = EnrollmentStatus.Enrolled,
                    StudentId = assignment.StudentId,
                    StudentCareerId = assignment.StudentCareerId,
                    CourseId = course.CourseId,
                    StudyPlanCourseId = course.Id,
                    CourseSectionId = sections.FirstOrDefault(s => s.CourseId == course.CourseId && s.Semester == enrollmentSemester)?.Id,
                    Shift = division.Shift
                });
                created++;
            }
        }
        await db.SaveChangesAsync(ct);
        return new BulkEnrollResult(assignments.Count, created, already,
            planCourses.Select(c => c.CourseName).Distinct().OrderBy(n => n).ToArray());
    }
}
