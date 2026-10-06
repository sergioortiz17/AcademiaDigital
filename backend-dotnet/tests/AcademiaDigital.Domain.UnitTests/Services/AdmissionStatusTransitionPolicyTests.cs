using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AdmissionStatusTransitionPolicyTests
{
    private readonly AdmissionStatusTransitionPolicy _policy = new();

    [Theory]
    [InlineData(AdmissionApplicationStatus.PreEnrolled, AdmissionApplicationStatus.Enrolled)]
    [InlineData(AdmissionApplicationStatus.PreEnrolled, AdmissionApplicationStatus.Waitlisted)]
    [InlineData(AdmissionApplicationStatus.Waitlisted, AdmissionApplicationStatus.PreEnrolled)]
    [InlineData(AdmissionApplicationStatus.Enrolled, AdmissionApplicationStatus.Confirmed)]
    [InlineData(AdmissionApplicationStatus.Enrolled, AdmissionApplicationStatus.Expired)]
    public void EnsureCanTransition_accepts_supported_transitions(
        AdmissionApplicationStatus current,
        AdmissionApplicationStatus target)
        => _policy.EnsureCanTransition(current, target, null);

    [Theory]
    [InlineData(AdmissionApplicationStatus.Confirmed, AdmissionApplicationStatus.Enrolled)]
    [InlineData(AdmissionApplicationStatus.Expired, AdmissionApplicationStatus.PreEnrolled)]
    [InlineData(AdmissionApplicationStatus.Rejected, AdmissionApplicationStatus.PreEnrolled)]
    [InlineData(AdmissionApplicationStatus.PreEnrolled, AdmissionApplicationStatus.PreEnrolled)]
    public void EnsureCanTransition_rejects_terminal_or_unsupported_transitions(
        AdmissionApplicationStatus current,
        AdmissionApplicationStatus target)
        => Assert.Throws<InvalidOperationException>(() =>
            _policy.EnsureCanTransition(current, target, null));

    [Fact]
    public void EnsureCanTransition_requires_a_reason_for_rejection()
        => Assert.Throws<ArgumentException>(() =>
            _policy.EnsureCanTransition(
                AdmissionApplicationStatus.PreEnrolled,
                AdmissionApplicationStatus.Rejected,
                " "));

    [Fact]
    public void EnsureCanTransition_rejects_a_reason_longer_than_500_characters()
        => Assert.Throws<ArgumentException>(() =>
            _policy.EnsureCanTransition(
                AdmissionApplicationStatus.PreEnrolled,
                AdmissionApplicationStatus.Enrolled,
                new string('x', 501)));

    [Fact]
    public void EnsureCanTransition_rejects_an_unknown_target_status()
        => Assert.Throws<ArgumentException>(() =>
            _policy.EnsureCanTransition(
                AdmissionApplicationStatus.PreEnrolled,
                (AdmissionApplicationStatus)999,
                null));
}
