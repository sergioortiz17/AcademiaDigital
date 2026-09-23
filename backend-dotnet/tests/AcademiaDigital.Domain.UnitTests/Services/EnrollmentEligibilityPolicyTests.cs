using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class EnrollmentEligibilityPolicyTests
{
    private readonly EnrollmentEligibilityPolicy _policy = new(new CourseEligibilityService());

    [Fact]
    public void EnsureCanEnroll_rejects_a_missing_strict_prerequisite()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _policy.EnsureCanEnroll(
            [PlanCourse(2)],
            [Prerequisite(2, 1, PrerequisiteType.Strict, MinimumRequiredStatus.Approved)],
            []));

        Assert.Contains("2->1 (Approved)", exception.Message);
    }

    [Fact]
    public void EnsureCanEnroll_accepts_a_satisfied_strict_prerequisite()
        => _policy.EnsureCanEnroll(
            [PlanCourse(2)],
            [Prerequisite(2, 1, PrerequisiteType.Strict, MinimumRequiredStatus.Regularized)],
            [Enrollment(1, EnrollmentStatus.Regularized)]);

    [Fact]
    public void EnsureCanEnroll_does_not_block_on_a_missing_soft_prerequisite()
        => _policy.EnsureCanEnroll(
            [PlanCourse(2)],
            [Prerequisite(2, 1, PrerequisiteType.Soft, MinimumRequiredStatus.Approved)],
            []);

    [Theory]
    [InlineData(EnrollmentStatus.Enrolled)]
    [InlineData(EnrollmentStatus.Regularized)]
    [InlineData(EnrollmentStatus.Approved)]
    [InlineData(EnrollmentStatus.Promoted)]
    public void EnsureCanEnroll_rejects_courses_already_approved_or_in_progress(EnrollmentStatus status)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => _policy.EnsureCanEnroll(
            [PlanCourse(2)], [], [Enrollment(2, status)]));

        Assert.Contains("2", exception.Message);
    }

    private static StudyPlanCourse PlanCourse(int courseId)
        => new() { Id = courseId * 10, CourseId = courseId, StudyPlanId = 7 };

    private static CoursePrerequisite Prerequisite(
        int courseId,
        int prerequisiteCourseId,
        PrerequisiteType type,
        MinimumRequiredStatus requiredStatus)
        => DomainTestFactory.Prerequisite(courseId, prerequisiteCourseId, type: type, requiredStatus: requiredStatus);

    private static Enrollment Enrollment(int courseId, EnrollmentStatus status)
        => new()
        {
            CourseId = courseId,
            Status = status,
            AcademicYear = 2026,
            Semester = 1,
            EnrollmentDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
