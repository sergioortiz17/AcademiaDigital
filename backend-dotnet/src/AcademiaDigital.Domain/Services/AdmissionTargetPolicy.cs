using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class AdmissionTargetPolicy
{
    public void Validate(Career career, Commission? commission, int? capacity)
    {
        if (commission is null)
            return;
        if (!commission.IsActive)
            throw new InvalidOperationException("Los formularios de admisión no pueden apuntar a una comisión inactiva.");
        if (commission.CareerId != career.Id)
            throw new InvalidOperationException("La carrera y la comisión del formulario de admisión son incompatibles.");
        ValidateCapacity(commission.Id, capacity);
    }

    public void ValidateCapacity(int? commissionId, int? capacity)
    {
        if (commissionId.HasValue && !capacity.HasValue)
            throw new ArgumentException("La capacidad es obligatoria cuando un formulario de admisión apunta a una comisión.");
    }
}
