using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AdmissionCapacityPolicyTests
{
    private readonly AdmissionCapacityPolicy _policy = new();

    [Theory]
    [InlineData(null, 500, true)]
    [InlineData(2, 0, true)]
    [InlineData(2, 1, true)]
    [InlineData(2, 2, false)]
    [InlineData(2, 3, false)]
    public void HasAvailableSlot_applies_limited_and_unlimited_capacity(
        int? capacity,
        int occupied,
        bool expected)
        => Assert.Equal(expected, _policy.HasAvailableSlot(capacity, occupied));

    [Theory]
    [InlineData(0)]
    [InlineData(100001)]
    public void ValidateCapacity_rejects_out_of_range_values(int capacity)
        => Assert.Throws<ArgumentException>(() => _policy.ValidateCapacity(capacity));

    [Theory]
    [InlineData(AdmissionApplicationStatus.PreEnrolled, true)]
    [InlineData(AdmissionApplicationStatus.Enrolled, true)]
    [InlineData(AdmissionApplicationStatus.Confirmed, true)]
    [InlineData(AdmissionApplicationStatus.Waitlisted, false)]
    [InlineData(AdmissionApplicationStatus.Expired, false)]
    [InlineData(AdmissionApplicationStatus.Rejected, false)]
    public void OccupiesCapacity_identifies_reserved_and_confirmed_states(
        AdmissionApplicationStatus status,
        bool expected)
        => Assert.Equal(expected, AdmissionCapacityPolicy.OccupiesCapacity(status));
}
