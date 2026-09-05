using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence.Repositories;

public sealed class TeacherAssignmentRepository(AppDbContext db) : ITeacherAssignmentRepository
{
    public async Task<IReadOnlyList<TeacherAssignment>> GetByTeacherAsync(
        long teacherId,
        bool includeEnded,
        CancellationToken ct = default)
        => await Details()
            .Where(assignment => assignment.TeacherId == teacherId
                && (includeEnded || assignment.IsCurrent))
            .OrderByDescending(assignment => assignment.StartedOn)
            .ThenByDescending(assignment => assignment.Id)
            .ToArrayAsync(ct);

    public Task<bool> HasHistoryForPositionAsync(int teachingPositionId, CancellationToken ct = default)
        => db.TeacherAssignments.AsNoTracking()
            .AnyAsync(assignment => assignment.TeachingPositionId == teachingPositionId, ct);

    public Task<TeacherAssignment?> FindAsync(
        long teacherId,
        long assignmentId,
        CancellationToken ct = default)
        => Details().SingleOrDefaultAsync(assignment => assignment.Id == assignmentId
            && assignment.TeacherId == teacherId, ct);

    public async Task<TeacherAssignment> AssignAsync(
        TeacherAssignment assignment,
        CancellationToken ct = default)
    {
        var position = await db.TeachingPositions
            .FromSqlInterpolated($"SELECT * FROM \"TeachingPositions\" WHERE id = {assignment.TeachingPositionId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teaching position not found.");
        var teacher = await db.Teachers
            .FromSqlInterpolated($"SELECT * FROM \"Teachers\" WHERE id = {assignment.TeacherId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teacher not found.");
        var teacherUser = await db.Users
            .FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE id = {teacher.UserId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teacher user not found.");
        if (!position.IsActive || !position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("The teaching position is not available.");
        if (!teacher.IsActive || !teacherUser.IsActive)
            throw new InvalidOperationException("The teacher is inactive.");

        position.TeacherId = assignment.TeacherId;
        position.IsVacant = false;
        position.UpdatedAt = assignment.CreatedAt;
        db.TeacherAssignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        return (await Details().SingleAsync(item => item.Id == assignment.Id, ct));
    }

    public async Task<TeacherAssignment> EndAsync(
        long teacherId,
        long assignmentId,
        DateOnly endedOn,
        DateTime endedAt,
        long actorUserId,
        string reason,
        CancellationToken ct = default)
    {
        var assignment = await db.TeacherAssignments
            .FromSqlInterpolated($"SELECT * FROM \"TeacherAssignments\" WHERE id = {assignmentId} FOR UPDATE")
            .SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Teacher assignment not found.");
        if (assignment.TeacherId != teacherId)
            throw new KeyNotFoundException("Teacher assignment not found.");
        if (!assignment.IsCurrent || assignment.EndedOn.HasValue)
            throw new InvalidOperationException("The teacher assignment is already closed.");

        var position = await db.TeachingPositions
            .FromSqlInterpolated($"SELECT * FROM \"TeachingPositions\" WHERE id = {assignment.TeachingPositionId} FOR UPDATE")
            .SingleAsync(ct);
        if (position.IsVacant || position.TeacherId != teacherId)
            throw new InvalidOperationException("Teaching position and current assignment are inconsistent.");
        if (endedOn < assignment.StartedOn)
            throw new ArgumentException("Assignment end date cannot precede its start date.");

        assignment.IsCurrent = false;
        assignment.EndedOn = endedOn;
        assignment.EndedAt = endedAt;
        assignment.EndedByUserId = actorUserId;
        assignment.EndReason = reason.Trim();
        position.TeacherId = null;
        position.IsVacant = true;
        position.UpdatedAt = endedAt;
        await db.SaveChangesAsync(ct);
        return await Details().SingleAsync(item => item.Id == assignment.Id, ct);
    }

    private IQueryable<TeacherAssignment> Details()
        => db.TeacherAssignments.AsNoTracking()
            .Include(assignment => assignment.Teacher).ThenInclude(teacher => teacher.User)
            .Include(assignment => assignment.TeachingPosition).ThenInclude(position => position.Course)
            .Include(assignment => assignment.TeachingPosition).ThenInclude(position => position.Commission);
}
