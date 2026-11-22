using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class ExamSchedulePlanner(AppDbContext db) : IExamSchedulePlanner
{
    public async Task<IReadOnlyList<ExamProposalRow>> ProposeAsync(
        int careerId, int academicYear, IReadOnlyList<DateOnly> availableDays, IReadOnlyCollection<int>? yearNumbers,
        CancellationToken ct = default)
    {
        if (availableDays.Count == 0) throw new ArgumentException("El rango elegido no tiene días hábiles.");
        var planId = await db.StudyPlans.AsNoTracking()
            .Where(p => p.CareerId == careerId && p.IsActive && p.Status == StudyPlanStatus.Active)
            .OrderByDescending(p => p.VersionNumber).Select(p => (int?)p.Id).FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("La carrera no tiene un plan de estudios vigente.");

        var courses = await db.StudyPlanCourses.AsNoTracking()
            .Where(c => c.StudyPlanId == planId && c.IsActive && (yearNumbers == null || yearNumbers.Count == 0 || yearNumbers.Contains(c.YearNumber)))
            .Select(c => new { c.CourseId, c.Course.Code, c.Course.Name, c.YearNumber, c.SortOrder })
            .ToListAsync(ct);
        var prerequisites = await db.CoursePrerequisites.AsNoTracking()
            .Where(p => p.StudyPlanId == planId && p.IsActive)
            .Select(p => new { p.CourseId, p.PrerequisiteCourseId })
            .ToListAsync(ct);

        var schedule = ExamScheduler.Propose(courses.Select(c => new SchedulableCourse(
            c.CourseId, c.YearNumber, c.SortOrder,
            prerequisites.Where(p => p.CourseId == c.CourseId).Select(p => p.PrerequisiteCourseId).ToArray())).ToArray(),
            availableDays);

        // Tribunal sugerido: los docentes que dictan la materia en el año (titular como presidente).
        var courseIds = courses.Select(c => c.CourseId).ToArray();
        var teachers = await db.TeacherAssignments.AsNoTracking()
            .Where(a => a.IsCurrent && courseIds.Contains(a.CourseSection.CourseId) && a.CourseSection.AcademicYear == academicYear)
            .Select(a => new
            {
                a.CourseSection.CourseId, a.TeacherId, a.CourseSection.PositionType,
                Name = (a.Teacher.User.Username + " " + a.Teacher.User.LastName).Trim()
            })
            .ToListAsync(ct);

        return courses.Select(c =>
        {
            var slot = schedule.Single(s => s.CourseId == c.CourseId);
            var tribunal = teachers.Where(t => t.CourseId == c.CourseId)
                .GroupBy(t => t.TeacherId).Select(g => g.OrderBy(t => t.PositionType).First())
                .OrderBy(t => t.PositionType).Take(5)
                .Select((t, i) => new TribunalSuggestion(t.TeacherId, t.Name, i == 0 ? "President" : "Vocal"))
                .ToArray();
            return new ExamProposalRow(c.CourseId, c.Code, c.Name, c.YearNumber, slot.Date, slot.Reason, tribunal);
        }).OrderBy(r => r.Date ?? DateOnly.MaxValue).ThenBy(r => r.YearNumber).ToArray();
    }
}
