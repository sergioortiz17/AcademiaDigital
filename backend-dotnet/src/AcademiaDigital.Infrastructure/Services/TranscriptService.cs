using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Services;

public sealed class TranscriptService(AppDbContext db) : ITranscriptService
{
    public async Task<TranscriptDto> GetAsync(long studentId, CancellationToken ct = default)
    {
        var s = await db.Students.AsNoTracking().Include(x => x.User).Include(x => x.Career)
            .SingleOrDefaultAsync(x => x.Id == studentId, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        var plan = await db.StudentStudyPlans.AsNoTracking()
            .Where(p => p.StudentId == studentId && p.IsCurrent)
            .Select(p => new { p.StudyPlanId, p.StudyPlan.Name, p.StudentCareerId, p.StudentCareer.AdmissionYear })
            .FirstOrDefaultAsync(ct);

        var planCourses = plan is null ? [] : await db.StudyPlanCourses.AsNoTracking()
            .Where(c => c.StudyPlanId == plan.StudyPlanId && c.IsActive)
            .OrderBy(c => c.YearNumber).ThenBy(c => c.SortOrder)
            .Select(c => new { c.CourseId, c.Course.Code, c.Course.Name, c.YearNumber, c.IsAnnual, c.Semester })
            .ToListAsync(ct);
        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => new { e.Id, e.CourseId, e.AcademicYear, e.Semester, e.Status, e.FinalGrade, e.EnrollmentDate })
            .ToListAsync(ct);
        // Mesas aprobadas: la nota y la fecha salen de la mesa publicada; el libro y folio, de su acta.
        var passedExams = await db.ExamRegistrations.AsNoTracking()
            .Where(r => r.StudentId == studentId && r.ExamTable.Status == ExamTableStatus.Published
                && r.GradeRevisions.Any(g => g.IsCurrent && g.Outcome == ExamResultOutcome.Passed))
            .Select(r => new
            {
                r.ExamTable.CourseId, r.ExamTableId, r.ExamTable.ExamDateUtc, r.ExamTable.ActBook, r.ExamTable.ActFolio,
                Grade = r.GradeRevisions.Where(g => g.IsCurrent).Select(g => g.Grade).FirstOrDefault()
            })
            .ToListAsync(ct);

        var courses = planCourses.Select(c =>
        {
            var latest = enrollments.Where(e => e.CourseId == c.CourseId)
                .OrderByDescending(e => e.AcademicYear).ThenByDescending(e => e.Semester).FirstOrDefault();
            var exam = passedExams.Where(e => e.CourseId == c.CourseId).OrderByDescending(e => e.ExamDateUtc).FirstOrDefault();
            var period = c.IsAnnual ? "Anual" : $"{c.Semester}.º cuatrimestre";
            if (exam is not null)
                return new TranscriptCourse(c.CourseId, c.Code, c.Name, c.YearNumber, period, "Aprobada por examen",
                    exam.Grade ?? latest?.FinalGrade, DateOnly.FromDateTime(exam.ExamDateUtc), exam.ExamTableId, exam.ActBook, exam.ActFolio);
            var condition = latest?.Status switch
            {
                EnrollmentStatus.Promoted => "Promocionada",
                EnrollmentStatus.Approved => "Aprobada",
                EnrollmentStatus.Regularized => "Regular",
                EnrollmentStatus.Failed => "Libre",
                EnrollmentStatus.Enrolled => "Cursando",
                EnrollmentStatus.Withdrawn => "Baja",
                _ => "Pendiente"
            };
            var approved = latest?.Status is EnrollmentStatus.Promoted or EnrollmentStatus.Approved;
            return new TranscriptCourse(c.CourseId, c.Code, c.Name, c.YearNumber, period, condition,
                approved ? latest!.FinalGrade : null, null, null, null, null);
        }).ToArray();

        var grades = courses.Where(c => c.FinalGrade.HasValue && c.Condition is "Promocionada" or "Aprobada" or "Aprobada por examen")
            .Select(c => c.FinalGrade!.Value).ToArray();
        var u = s.User;
        return new TranscriptDto(
            new TranscriptStudent(s.Id, u.LastName, u.Username, u.Dni, u.Cuil, u.Gender,
                u.BirthDate.HasValue ? DateOnly.FromDateTime(u.BirthDate.Value) : null,
                u.AddressLine, u.City, u.Province,
                string.IsNullOrEmpty(u.Phone) ? null : $"{u.PhoneCode} {u.Phone}".Trim(), u.Email,
                s.LegajoNumber, s.RegistryBook, s.RegistryFolio, StatusLabel(s.Status), plan?.AdmissionYear,
                s.GraduatedOn, s.Career.Name, plan?.Name),
            courses,
            grades.Length,
            grades.Length == 0 ? null : Math.Round(grades.Average(), 2));
    }

    public async Task SetRegistryAsync(long studentId, string? book, string? folio, CancellationToken ct = default)
    {
        var student = await db.Students.SingleOrDefaultAsync(x => x.Id == studentId, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        student.RegistryBook = Clean(book, "El libro matriz");
        student.RegistryFolio = Clean(folio, "El folio");
        student.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetExamActAsync(long examTableId, string? book, string? folio, CancellationToken ct = default)
    {
        var table = await db.ExamTables.SingleOrDefaultAsync(x => x.Id == examTableId, ct)
            ?? throw new KeyNotFoundException("Mesa de examen no encontrada.");
        table.ActBook = Clean(book, "El libro del acta");
        table.ActFolio = Clean(folio, "El folio del acta");
        await db.SaveChangesAsync(ct);
    }

    private static string? Clean(string? value, string field)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > 30) throw new ArgumentException($"{field} admite hasta 30 caracteres.");
        return trimmed;
    }

    private static string StatusLabel(StudentStatus status) => status switch
    {
        StudentStatus.Regular => "Regular",
        StudentStatus.Libre => "Libre",
        StudentStatus.Graduated => "Egresado",
        StudentStatus.Withdrawn => "Baja",
        _ => status.ToString()
    };
}
