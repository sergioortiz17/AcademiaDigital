using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class CourseEligibilityServiceTests
{
    private readonly CourseEligibilityService _service = new();

    [Theory]
    [InlineData(EnrollmentStatus.Approved, true)]
    [InlineData(EnrollmentStatus.Promoted, true)]
    [InlineData(EnrollmentStatus.Enrolled, false)]
    [InlineData(EnrollmentStatus.Regularized, false)]
    [InlineData(EnrollmentStatus.Failed, false)]
    [InlineData(EnrollmentStatus.Withdrawn, false)]
    public void IsApproved_classifies_only_completed_statuses(EnrollmentStatus status, bool expected)
        => Assert.Equal(expected, _service.IsApproved(status));

    [Theory]
    [InlineData(EnrollmentStatus.Enrolled, true)]
    [InlineData(EnrollmentStatus.Regularized, true)]
    [InlineData(EnrollmentStatus.Approved, false)]
    [InlineData(EnrollmentStatus.Promoted, false)]
    [InlineData(EnrollmentStatus.Failed, false)]
    [InlineData(EnrollmentStatus.Withdrawn, false)]
    public void IsInProgress_classifies_only_active_academic_statuses(EnrollmentStatus status, bool expected)
        => Assert.Equal(expected, _service.IsInProgress(status));

    [Theory]
    [InlineData(null, MinimumRequiredStatus.Approved, false)]
    [InlineData(EnrollmentStatus.Approved, MinimumRequiredStatus.Approved, true)]
    [InlineData(EnrollmentStatus.Promoted, MinimumRequiredStatus.Approved, true)]
    [InlineData(EnrollmentStatus.Regularized, MinimumRequiredStatus.Approved, false)]
    [InlineData(EnrollmentStatus.Promoted, MinimumRequiredStatus.Promoted, true)]
    [InlineData(EnrollmentStatus.Approved, MinimumRequiredStatus.Promoted, false)]
    [InlineData(EnrollmentStatus.Regularized, MinimumRequiredStatus.Regularized, true)]
    [InlineData(EnrollmentStatus.Approved, MinimumRequiredStatus.Regularized, true)]
    [InlineData(EnrollmentStatus.Promoted, MinimumRequiredStatus.Regularized, true)]
    [InlineData(EnrollmentStatus.Enrolled, MinimumRequiredStatus.Regularized, false)]
    public void SatisfiesMinimumStatus_applies_the_required_academic_threshold(
        EnrollmentStatus? current,
        MinimumRequiredStatus required,
        bool expected)
        => Assert.Equal(expected, _service.SatisfiesMinimumStatus(current, required));
}
