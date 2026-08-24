using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class GradebookRepository(AppDbContext db) : IGradebookRepository
{
    public Task<bool> CanTeacherManagePositionAsync(long userId, int teachingPositionId, CancellationToken ct = default)
        => db.TeacherAssignments.AsNoTracking().AnyAsync(assignment =>
            assignment.Teacher.UserId == userId
            && assignment.TeachingPositionId == teachingPositionId
            && assignment.IsCurrent, ct);

    public Task<bool> CanTeacherManageGradebookAsync(long userId, long gradebookId, CancellationToken ct = default)
        => db.Gradebooks.AsNoTracking().AnyAsync(gradebook => gradebook.Id == gradebookId
            && db.TeacherAssignments.Any(assignment =>
                assignment.Teacher.UserId == userId
                && assignment.TeachingPositionId == gradebook.TeachingPositionId
                && assignment.IsCurrent), ct);

    public async Task<IReadOnlyList<Gradebook>> GetGradebooksAsync(
        int? academicYear,
        int? courseId,
        int? commissionId,
        long? teacherUserId,
        CancellationToken ct = default)
    {
        var query = Details();
        if (academicYear.HasValue) query = query.Where(item => item.AcademicYear == academicYear);
        if (courseId.HasValue) query = query.Where(item => item.CourseId == courseId);
        if (commissionId.HasValue) query = query.Where(item => item.CommissionId == commissionId);
        if (teacherUserId.HasValue)
            query = query.Where(gradebook => db.TeacherAssignments.Any(assignment =>
                assignment.Teacher.UserId == teacherUserId
                && assignment.TeachingPositionId == gradebook.TeachingPositionId
                && assignment.IsCurrent));
        return await query.OrderByDescending(item => item.AcademicYear)
            .ThenByDescending(item => item.Semester)
            .ToArrayAsync(ct);
    }

    public Task<Gradebook?> FindAsync(long gradebookId, CancellationToken ct = default)
        => Details().SingleOrDefaultAsync(item => item.Id == gradebookId, ct);

    public async Task<Gradebook?> FindForUpdateAsync(long gradebookId, CancellationToken ct = default)
        => await db.Gradebooks
            .FromSqlInterpolated($"SELECT * FROM [Gradebooks] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {gradebookId}")
            .Include(item => item.Evaluations)
            .Include(item => item.GradeRevisions).ThenInclude(item => item.Evaluation)
            .Include(item => item.GradeRevisions).ThenInclude(item => item.Enrollment)
                .ThenInclude(item => item.StudyPlanCourse)!.ThenInclude(item => item!.ApprovalRule)
            .Include(item => item.Reopenings)
            .SingleOrDefaultAsync(ct);

    public async Task<(Gradebook Gradebook, bool Created)> CreateIdempotentAsync(Gradebook gradebook, CancellationToken ct = default)
    {
        _ = await db.TeachingPositions
            .FromSqlInterpolated($"SELECT * FROM [TeachingPositions] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {gradebook.TeachingPositionId}")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teaching position not found.");
        var existing = await db.Gradebooks.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == gradebook.IdempotencyKey, ct);
        if (existing is not null)
        {
            var loaded = await Details().SingleAsync(item => item.Id == existing.Id, ct);
            var sameEvaluations = loaded.Evaluations.OrderBy(item => item.DisplayOrder)
                .Select(item => (item.Name, item.WeightPercentage, item.MaximumScore))
                .SequenceEqual(gradebook.Evaluations.OrderBy(item => item.DisplayOrder)
                    .Select(item => (item.Name, item.WeightPercentage, item.MaximumScore)));
            if (existing.TeachingPositionId != gradebook.TeachingPositionId || !sameEvaluations)
                throw new InvalidOperationException("The idempotency key was already used with a different gradebook.");
            return (loaded, false);
        }
        if (await db.Gradebooks.AsNoTracking().AnyAsync(item =>
                item.CourseId == gradebook.CourseId
                && item.CommissionId == gradebook.CommissionId
                && item.AcademicYear == gradebook.AcademicYear
                && item.Semester == gradebook.Semester, ct))
            throw new InvalidOperationException("A gradebook already exists for this course offering.");
        db.Gradebooks.Add(gradebook);
        await db.SaveChangesAsync(ct);
        return (await Details().SingleAsync(item => item.Id == gradebook.Id, ct), true);
    }

    public async Task<IReadOnlyList<GradebookRosterRow>> GetRosterAsync(Gradebook gradebook, CancellationToken ct = default)
        => await db.Enrollments.AsNoTracking()
            .Where(enrollment => enrollment.CourseId == gradebook.CourseId
                && enrollment.AcademicYear == gradebook.AcademicYear
                && enrollment.Semester == gradebook.Semester
                && enrollment.Status != EnrollmentStatus.Withdrawn
                && (enrollment.TeachingPositionId == gradebook.TeachingPositionId
                    || (enrollment.TeachingPositionId == null && db.StudentAcademicAssignments.Any(assignment =>
                        assignment.StudentCareerId == enrollment.StudentCareerId
                        && assignment.CommissionId == gradebook.CommissionId
                        && assignment.AcademicYear == gradebook.AcademicYear))))
            .OrderBy(enrollment => enrollment.Student.User.LastName)
            .ThenBy(enrollment => enrollment.Student.User.Username)
            .Select(enrollment => new GradebookRosterRow(
                enrollment.Id,
                enrollment.StudentId,
                (enrollment.Student.User.Username + " " + enrollment.Student.User.LastName).Trim(),
                enrollment.Student.LegajoNumber,
                enrollment.Student.User.Dni ?? string.Empty))
            .ToArrayAsync(ct);

    public async Task SaveGradeRevisionsAsync(IReadOnlyList<GradeEntryRevision> revisions, CancellationToken ct = default)
    {
        foreach (var revision in revisions)
        {
            var previous = await db.GradeEntryRevisions
                .Where(item => item.EvaluationId == revision.EvaluationId && item.EnrollmentId == revision.EnrollmentId)
                .OrderByDescending(item => item.Version)
                .FirstOrDefaultAsync(ct);
            if (previous is not null) previous.IsCurrent = false;
            revision.Version = (previous?.Version ?? 0) + 1;
            db.GradeEntryRevisions.Add(revision);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(Gradebook gradebook, CancellationToken ct = default)
        => _ = await db.SaveChangesAsync(ct);

    public async Task ApplyFinalResultsAsync(
        Gradebook gradebook,
        IReadOnlyList<EnrollmentGradebookResult> results,
        CancellationToken ct = default)
    {
        foreach (var result in results)
        {
            var enrollment = await db.Enrollments
                .FromSqlInterpolated($"SELECT * FROM [Enrollments] WITH (UPDLOCK, HOLDLOCK) WHERE [id] = {result.EnrollmentId}")
                .SingleAsync(ct);
            enrollment.FinalGrade = result.Average;
            enrollment.Status = result.Status;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Gradebook>> GetPublishedForStudentAsync(
        long studentId,
        int? courseId,
        CancellationToken ct = default)
    {
        var query = Details().Where(item =>
            item.Status == GradebookStatus.Published || item.Status == GradebookStatus.Closed);
        query = query.Where(item => item.GradeRevisions.Any(revision => revision.StudentId == studentId && revision.IsCurrent));
        if (courseId.HasValue) query = query.Where(item => item.CourseId == courseId);
        return await query.OrderByDescending(item => item.AcademicYear).ThenByDescending(item => item.Semester).ToArrayAsync(ct);
    }

    private IQueryable<Gradebook> Details()
        => db.Gradebooks.AsNoTracking()
            .Include(item => item.Course)
            .Include(item => item.Commission)
            .Include(item => item.Evaluations)
            .Include(item => item.GradeRevisions.Where(revision => revision.IsCurrent)).ThenInclude(item => item.Evaluation)
            .Include(item => item.GradeRevisions.Where(revision => revision.IsCurrent))
            .ThenInclude(revision => revision.Enrollment).ThenInclude(enrollment => enrollment.StudyPlanCourse)!.ThenInclude(item => item!.ApprovalRule)
            .Include(item => item.Reopenings);
}
