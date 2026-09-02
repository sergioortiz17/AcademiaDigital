using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class StudentRematriculationPolicyTests
{
    private readonly StudentRematriculationPolicy policy = new();

    [Theory]
    [InlineData(StudentStatus.Regular)]
    [InlineData(StudentStatus.Libre)]
    public void ValidateStudent_accepts_active_non_terminal_students(StudentStatus status)
        => policy.ValidateStudent(new Student { Status = status, User = new User { IsActive = true } });

    [Theory]
    [InlineData(StudentStatus.Graduated)]
    [InlineData(StudentStatus.Withdrawn)]
    public void ValidateStudent_rejects_terminal_statuses(StudentStatus status)
        => Assert.Throws<InvalidOperationException>(() =>
            policy.ValidateStudent(new Student { Status = status, User = new User { IsActive = true } }));

    [Fact]
    public void ValidateStudent_rejects_an_inactive_user()
        => Assert.Throws<InvalidOperationException>(() => policy.ValidateStudent(
            new Student { Status = StudentStatus.Regular, User = new User { IsActive = false } }));

    [Fact]
    public void ValidateTarget_accepts_matching_active_entities()
        => policy.ValidateTarget(
            new StudentCareer { CareerId = 10, IsActive = true },
            new StudyPlan { CareerId = 10, IsActive = true, Status = StudyPlanStatus.Active },
            new Commission { CareerId = 10, AcademicYear = 2027, YearNumber = 2, IsActive = true },
            2027,
            2);

    [Fact]
    public void ValidateTarget_rejects_an_incompatible_commission_cycle()
        => Assert.Throws<InvalidOperationException>(() => policy.ValidateTarget(
            new StudentCareer { CareerId = 10, IsActive = true },
            new StudyPlan { CareerId = 10, IsActive = true, Status = StudyPlanStatus.Active },
            new Commission { CareerId = 10, AcademicYear = 2026, YearNumber = 1, IsActive = true },
            2027,
            2));

    [Theory]
    [InlineData(null, 2027)]
    [InlineData(2026, 2026)]
    [InlineData(2026, 2028)]
    public void ValidateNextCycle_rejects_missing_same_or_skipped_cycles(int? latest, int target)
        => Assert.Throws<InvalidOperationException>(() => policy.ValidateNextCycle(latest, target));

    [Fact]
    public void ValidateNextCycle_accepts_the_immediately_following_year()
        => policy.ValidateNextCycle(2026, 2027);
}
