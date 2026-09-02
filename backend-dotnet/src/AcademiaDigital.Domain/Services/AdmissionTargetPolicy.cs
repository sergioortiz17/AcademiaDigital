using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionTargetPolicy
{
    public void Validate(Career career, Commission? commission, int? capacity)
    {
        if (commission is null)
            return;
        if (!commission.IsActive)
            throw new InvalidOperationException("Admission forms cannot target an inactive commission.");
        if (commission.CareerId != career.Id)
            throw new InvalidOperationException("Admission form career and commission are incompatible.");
        ValidateCapacity(commission.Id, capacity);
    }

    public void ValidateCapacity(int? commissionId, int? capacity)
    {
        if (commissionId.HasValue && !capacity.HasValue)
            throw new ArgumentException("Capacity is required when an admission form targets a commission.");
    }
}
