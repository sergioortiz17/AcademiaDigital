using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class TeachingAssignmentPolicyTests
{
    private readonly TeachingAssignmentPolicy policy = new();

    [Fact]
    public void ValidatePositionDefinition_accepts_a_compatible_course_and_commission()
        => policy.ValidatePositionDefinition(2027, 1, 40, Course(), Commission());

    [Fact]
    public void ValidatePositionDefinition_rejects_incompatible_academic_context()
    {
        var incompatibleCommission = Commission();
        incompatibleCommission.CareerId = 11;
        Assert.Throws<ArgumentException>(() =>
            policy.ValidatePositionDefinition(2027, 1, 40, Course(), incompatibleCommission));
        Assert.Throws<ArgumentException>(() =>
            policy.ValidatePositionDefinition(2028, 1, 40, Course(), Commission()));
        Assert.Throws<ArgumentException>(() =>
            policy.ValidatePositionDefinition(2027, 3, 40, Course(), Commission()));
    }

    [Fact]
    public void EnsureCanAssign_requires_an_active_vacant_position_and_teacher()
    {
        policy.EnsureCanAssign(Position(), Teacher(), new DateOnly(2027, 3, 1));
        var occupied = Position();
        occupied.IsVacant = false;
        occupied.TeacherId = 4;
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanAssign(
            occupied, Teacher(), new DateOnly(2027, 3, 1)));
        Assert.Throws<ArgumentException>(() => policy.EnsureCanAssign(
            Position(), Teacher(), new DateOnly(2028, 3, 1)));
    }

    [Fact]
    public void EnsureCanEnd_requires_current_history_valid_date_and_reason()
    {
        var assignment = new TeacherAssignment { StartedOn = new DateOnly(2027, 3, 1), IsCurrent = true };
        policy.EnsureCanEnd(assignment, new DateOnly(2027, 7, 1), "Replacement");
        Assert.Throws<ArgumentException>(() => policy.EnsureCanEnd(
            assignment, new DateOnly(2027, 2, 28), "Replacement"));
        Assert.Throws<ArgumentException>(() => policy.EnsureCanEnd(
            assignment, new DateOnly(2027, 7, 1), " "));
    }

    [Fact]
    public void EnsureCanDeactivate_rejects_an_assigned_position()
    {
        var occupied = Position();
        occupied.IsVacant = false;
        occupied.TeacherId = 4;
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanDeactivate(occupied));
    }

    [Fact]
    public void EnsurePositionCanChange_rejects_assignment_history()
        => Assert.Throws<InvalidOperationException>(() => policy.EnsurePositionCanChange(Position(), true));

    private static Course Course() => new() { Id = 2, CareerId = 10, IsActive = true };
    private static Commission Commission() => new()
    {
        Id = 3, CareerId = 10, AcademicYear = 2027, IsActive = true
    };
    private static TeachingPosition Position() => new()
    {
        Id = 5, AcademicYear = 2027, IsActive = true, IsVacant = true
    };
    private static Teacher Teacher() => new()
    {
        Id = 4,
        IsActive = true,
        User = new User { IsActive = true, Role = UserRole.Profesor }
    };
}
