using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class TeachingAssignmentPolicy
{
    public void ValidatePositionDefinition(
        int academicYear,
        int semester,
        int maxStudents,
        Course course,
        Commission commission)
    {
        if (academicYear is < 2000 or > 2100)
            throw new ArgumentException("Academic year must be between 2000 and 2100.");
        if (semester is not (1 or 2))
            throw new ArgumentException("Semester must be 1 or 2.");
        if (maxStudents is < 1 or > 1000)
            throw new ArgumentException("Maximum students must be between 1 and 1000.");
        if (!course.IsActive)
            throw new InvalidOperationException("The course is inactive.");
        if (!commission.IsActive)
            throw new InvalidOperationException("The commission is inactive.");
        if (commission.CareerId != course.CareerId)
            throw new ArgumentException("Course and commission must belong to the same career.");
        if (commission.AcademicYear != academicYear)
            throw new ArgumentException("Commission and teaching position must use the same academic year.");
    }

    public void EnsurePositionCanChange(TeachingPosition position, bool hasAssignmentHistory)
    {
        if (!position.IsActive)
            throw new InvalidOperationException("The teaching position is inactive.");
        if (!position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("An assigned teaching position cannot be changed.");
        if (hasAssignmentHistory)
            throw new InvalidOperationException("A teaching position with assignment history cannot change its academic definition.");
    }

    public void EnsureCanDeactivate(TeachingPosition position)
    {
        if (!position.IsActive) return;
        if (!position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("End the current teacher assignment before deactivating the position.");
    }

    public void EnsureCanAssign(TeachingPosition position, Teacher teacher, DateOnly startedOn)
    {
        if (!position.IsActive)
            throw new InvalidOperationException("The teaching position is inactive.");
        if (!position.IsVacant || position.TeacherId.HasValue)
            throw new InvalidOperationException("The teaching position is already assigned.");
        if (!teacher.IsActive || !teacher.User.IsActive)
            throw new InvalidOperationException("The teacher is inactive.");
        if (startedOn.Year != position.AcademicYear)
            throw new ArgumentException("Assignment start date must belong to the teaching position academic year.");
    }

    public void EnsureCanEnd(TeacherAssignment assignment, DateOnly endedOn, string reason)
    {
        if (!assignment.IsCurrent || assignment.EndedOn.HasValue)
            throw new InvalidOperationException("The teacher assignment is already closed.");
        if (endedOn < assignment.StartedOn)
            throw new ArgumentException("Assignment end date cannot precede its start date.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("An end reason is required.");
    }
}
