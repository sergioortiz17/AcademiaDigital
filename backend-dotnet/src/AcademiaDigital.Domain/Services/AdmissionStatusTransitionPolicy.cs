using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionStatusTransitionPolicy
{
    private static readonly IReadOnlyDictionary<AdmissionApplicationStatus, AdmissionApplicationStatus[]> AllowedTransitions
        = new Dictionary<AdmissionApplicationStatus, AdmissionApplicationStatus[]>
        {
            [AdmissionApplicationStatus.PreEnrolled] =
            [
                AdmissionApplicationStatus.Enrolled,
                AdmissionApplicationStatus.Waitlisted,
                AdmissionApplicationStatus.Expired,
                AdmissionApplicationStatus.Rejected
            ],
            [AdmissionApplicationStatus.Waitlisted] =
            [
                AdmissionApplicationStatus.PreEnrolled,
                AdmissionApplicationStatus.Expired,
                AdmissionApplicationStatus.Rejected
            ],
            [AdmissionApplicationStatus.Enrolled] =
            [
                AdmissionApplicationStatus.Confirmed,
                AdmissionApplicationStatus.Expired,
                AdmissionApplicationStatus.Rejected
            ]
        };

    public void EnsureCanTransition(
        AdmissionApplicationStatus current,
        AdmissionApplicationStatus target,
        string? reason)
    {
        if (!Enum.IsDefined(target))
            throw new ArgumentException("Admission target status is invalid.");
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
            throw new InvalidOperationException($"Admission status cannot transition from {current} to {target}.");
        if (target == AdmissionApplicationStatus.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required when rejecting an admission application.");
        if (reason?.Trim().Length > 500)
            throw new ArgumentException("Admission status reason cannot exceed 500 characters.");
    }
}
