using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionCapacityPolicy
{
    public void ValidateCapacity(int? capacity)
    {
        if (capacity is < 1 or > 100_000)
            throw new ArgumentException("La capacidad del formulario de admisión debe estar entre 1 y 100000, o nula para capacidad ilimitada.");
    }

    public bool HasAvailableSlot(int? capacity, int occupied)
    {
        ValidateCapacity(capacity);
        if (occupied < 0)
            throw new ArgumentException("La capacidad de admisión ocupada no puede ser negativa.");
        return capacity is null || occupied < capacity.Value;
    }

    public static bool OccupiesCapacity(AdmissionApplicationStatus status)
        => status is AdmissionApplicationStatus.PreEnrolled
            or AdmissionApplicationStatus.Enrolled
            or AdmissionApplicationStatus.Confirmed;
}
