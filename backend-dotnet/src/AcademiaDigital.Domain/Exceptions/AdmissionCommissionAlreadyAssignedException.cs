namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionCommissionAlreadyAssignedException(int commissionId)
    : Exception($"La comisión {commissionId} ya tiene un formulario de admisión.");
