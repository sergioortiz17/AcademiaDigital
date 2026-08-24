using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionCapacityPolicy
{
    public void ValidateCapacity(int? capacity)
    {
        if (capacity is < 1 or > 100_000)
            throw new ArgumentException("Admission form capacity must be between 1 and 100000, or null for unlimited capacity.");
    }

    public bool HasAvailableSlot(int? capacity, int occupied)
    {
        ValidateCapacity(capacity);
        if (occupied < 0)
            throw new ArgumentException("Occupied admission capacity cannot be negative.");
        return capacity is null || occupied < capacity.Value;
    }

    public static bool OccupiesCapacity(AdmissionApplicationStatus status)
        => status is AdmissionApplicationStatus.PreEnrolled
            or AdmissionApplicationStatus.Enrolled
            or AdmissionApplicationStatus.Confirmed;
}
