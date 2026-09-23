using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class TeacherProfilePolicyTests
{
    private readonly TeacherProfilePolicy policy = new();
    private static readonly DateTime Now = new(2027, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NormalizeEmployeeNumber_trims_and_normalizes_case()
        => Assert.Equal("DOC-001", policy.NormalizeEmployeeNumber(" doc-001 "));

    [Fact]
    public void NormalizeEmployeeNumber_rejects_blank_values()
        => Assert.Throws<ArgumentException>(() => policy.NormalizeEmployeeNumber("  "));

    [Fact]
    public void ValidateHireDate_rejects_future_dates()
        => Assert.Throws<ArgumentException>(() => policy.ValidateHireDate(Now.AddDays(1), Now));

    [Fact]
    public void Deactivate_preserves_audit_data()
    {
        var teacher = new Teacher { IsActive = true };

        policy.Deactivate(teacher, 99, " End of appointment ", Now);

        Assert.False(teacher.IsActive);
        Assert.Equal(Now, teacher.DeactivatedAt);
        Assert.Equal(99, teacher.DeactivatedByUserId);
        Assert.Equal("End of appointment", teacher.DeactivationReason);
    }

    [Fact]
    public void Deactivate_is_idempotent()
    {
        var originalDate = Now.AddDays(-1);
        var teacher = new Teacher
        {
            IsActive = false,
            DeactivatedAt = originalDate,
            DeactivatedByUserId = 10,
            DeactivationReason = "Original"
        };

        policy.Deactivate(teacher, 99, "Replacement", Now);

        Assert.Equal(originalDate, teacher.DeactivatedAt);
        Assert.Equal(10, teacher.DeactivatedByUserId);
        Assert.Equal("Original", teacher.DeactivationReason);
    }
}
