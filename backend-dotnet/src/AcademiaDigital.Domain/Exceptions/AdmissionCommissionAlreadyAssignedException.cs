namespace AcademiaDigital.Domain.Exceptions;

public sealed class AdmissionCommissionAlreadyAssignedException(int commissionId)
    : Exception($"Commission {commissionId} already has an admission form.");
