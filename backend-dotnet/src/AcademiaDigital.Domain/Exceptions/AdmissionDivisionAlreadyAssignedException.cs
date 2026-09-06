namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionDivisionAlreadyAssignedException(int commissionId)
    : Exception($"La comisión {commissionId} ya tiene un formulario de admisión.");
