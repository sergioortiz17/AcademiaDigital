using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class ExamTableRepository(AppDbContext db) : IExamTableRepository
{
    public Task<bool> CanTeacherManageAsync(long userId, long examTableId, CancellationToken ct = default)
        => db.ExamTribunalMembers.AsNoTracking().AnyAsync(item =>
            item.ExamTableId == examTableId && item.Teacher.UserId == userId, ct);

    public async Task<IReadOnlyList<ExamTable>> GetAsync(
        int? academicYear,
        int? courseId,
        long? teacherUserId,
        CancellationToken ct = default)
    {
        var query = Details();
        if (academicYear.HasValue) query = query.Where(item => item.AcademicYear == academicYear);
        if (courseId.HasValue) query = query.Where(item => item.CourseId == courseId);
        if (teacherUserId.HasValue)
            query = query.Where(item => item.TribunalMembers.Any(member => member.Teacher.UserId == teacherUserId));
        return await query.OrderByDescending(item => item.ExamDateUtc).ToArrayAsync(ct);
    }

    public async Task<IReadOnlyList<ExamTable>> GetForStudentAsync(long studentId, CancellationToken ct = default)
        => await Details().Where(table =>
                table.Registrations.Any(registration => registration.StudentId == studentId)
                || (table.Status == ExamTableStatus.Open && db.Enrollments.Any(enrollment =>
                    enrollment.StudentId == studentId
                    && enrollment.CourseId == table.CourseId
                    && enrollment.Status == EnrollmentStatus.Regularized)))
            .OrderByDescending(item => item.ExamDateUtc)
            .ToArrayAsync(ct);

    public Task<ExamTable?> FindAsync(long examTableId, CancellationToken ct = default)
        => Details().SingleOrDefaultAsync(item => item.Id == examTableId, ct);

    public async Task<ExamTable?> FindForUpdateAsync(long examTableId, CancellationToken ct = default)
        => await db.ExamTables
            .FromSqlInterpolated($"SELECT * FROM \"ExamTables\" WHERE id = {examTableId} FOR UPDATE")
            .Include(item => item.TribunalMembers).ThenInclude(item => item.Teacher).ThenInclude(item => item.User)
            .Include(item => item.Registrations).ThenInclude(item => item.Enrollment).ThenInclude(item => item.StudyPlanCourse)!.ThenInclude(item => item!.ApprovalRule)
            .Include(item => item.Registrations).ThenInclude(item => item.GradeRevisions)
            .Include(item => item.Reopenings)
            .SingleOrDefaultAsync(ct);

    public async Task<(ExamTable ExamTable, bool Created)> CreateIdempotentAsync(ExamTable examTable, CancellationToken ct = default)
    {
        _ = await db.Courses
            .FromSqlInterpolated($"SELECT * FROM \"Courses\" WHERE id = {examTable.CourseId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Course not found.");
        var existing = await db.ExamTables.AsNoTracking()
            .SingleOrDefaultAsync(item => item.IdempotencyKey == examTable.IdempotencyKey, ct);
        if (existing is not null)
        {
            var loaded = await Details().SingleAsync(item => item.Id == existing.Id, ct);
            var sameTribunal = loaded.TribunalMembers.OrderBy(item => item.TeacherId)
                .Select(item => (item.TeacherId, item.Role))
                .SequenceEqual(examTable.TribunalMembers.OrderBy(item => item.TeacherId)
                    .Select(item => (item.TeacherId, item.Role)));
            if (existing.CourseId != examTable.CourseId
                || existing.ExamDateUtc != examTable.ExamDateUtc
                || existing.CallNumber != examTable.CallNumber
                || existing.RegistrationDeadlineUtc != examTable.RegistrationDeadlineUtc
                || !sameTribunal)
                throw new InvalidOperationException("The idempotency key was already used with a different exam table.");
            return (loaded, false);
        }
        if (await db.ExamTables.AsNoTracking().AnyAsync(item =>
                item.CourseId == examTable.CourseId
                && item.ExamDateUtc == examTable.ExamDateUtc
                && item.CallNumber == examTable.CallNumber, ct))
            throw new InvalidOperationException("An exam table already exists for this course, date and call.");
        db.ExamTables.Add(examTable);
        await db.SaveChangesAsync(ct);
        return (await Details().SingleAsync(item => item.Id == examTable.Id, ct), true);
    }

    public async Task<Enrollment?> FindEnrollmentForUpdateAsync(long enrollmentId, CancellationToken ct = default)
        => await db.Enrollments
            .FromSqlInterpolated($"SELECT * FROM \"Enrollments\" WHERE id = {enrollmentId} FOR UPDATE")
            .Include(item => item.Student).ThenInclude(item => item.User)
            .Include(item => item.StudyPlanCourse)!.ThenInclude(item => item!.ApprovalRule)
            .SingleOrDefaultAsync(ct);

    public async Task<ExamRegistration> RegisterAsync(
        ExamTable examTable,
        Enrollment enrollment,
        long actorUserId,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        var existing = await db.ExamRegistrations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ExamTableId == examTable.Id && item.EnrollmentId == enrollment.Id, ct);
        if (existing is not null) return existing;
        var attempt = (await db.ExamRegistrations
            .Where(item => item.EnrollmentId == enrollment.Id)
            .MaxAsync(item => (int?)item.AttemptNumber, ct) ?? 0) + 1;
        var registration = new ExamRegistration
        {
            ExamTableId = examTable.Id,
            EnrollmentId = enrollment.Id,
            StudentId = enrollment.StudentId,
            AttemptNumber = attempt,
            RegisteredAt = nowUtc,
            RegisteredByUserId = actorUserId
        };
        db.ExamRegistrations.Add(registration);
        await db.SaveChangesAsync(ct);
        return registration;
    }

    public async Task SaveGradeRevisionsAsync(IReadOnlyList<ExamGradeRevision> revisions, CancellationToken ct = default)
    {
        foreach (var revision in revisions)
        {
            var previous = await db.ExamGradeRevisions
                .Where(item => item.ExamRegistrationId == revision.ExamRegistrationId)
                .OrderByDescending(item => item.Version)
                .FirstOrDefaultAsync(ct);
            if (previous is not null) previous.IsCurrent = false;
            revision.Version = (previous?.Version ?? 0) + 1;
            db.ExamGradeRevisions.Add(revision);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveAsync(ExamTable examTable, CancellationToken ct = default)
        => _ = await db.SaveChangesAsync(ct);

    public async Task PublishAsync(ExamTable examTable, CancellationToken ct = default)
    {
        foreach (var registration in examTable.Registrations)
        {
            var enrollment = registration.Enrollment;
            var result = registration.GradeRevisions.Single(item => item.IsCurrent);
            if (!registration.ResultAppliedAt.HasValue)
            {
                registration.PreviousEnrollmentStatus = enrollment.Status;
                registration.PreviousFinalGrade = enrollment.FinalGrade;
            }
            else
            {
                enrollment.Status = registration.PreviousEnrollmentStatus ?? EnrollmentStatus.Regularized;
                enrollment.FinalGrade = registration.PreviousFinalGrade;
            }
            if (result.Outcome == ExamResultOutcome.Passed)
            {
                enrollment.Status = EnrollmentStatus.Approved;
                enrollment.FinalGrade = result.Grade;
            }
            registration.ResultAppliedAt = examTable.PublishedAt;
        }
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<ExamTable> Details()
        => db.ExamTables.AsNoTracking()
            .Include(item => item.Course)
            .Include(item => item.TribunalMembers).ThenInclude(item => item.Teacher).ThenInclude(item => item.User)
            .Include(item => item.Registrations).ThenInclude(item => item.Student).ThenInclude(item => item.User)
            .Include(item => item.Registrations).ThenInclude(item => item.Enrollment).ThenInclude(item => item.StudyPlanCourse)!.ThenInclude(item => item!.ApprovalRule)
            .Include(item => item.Registrations).ThenInclude(item => item.GradeRevisions.Where(revision => revision.IsCurrent))
            .Include(item => item.Reopenings);
}
