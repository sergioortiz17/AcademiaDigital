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
            throw new ArgumentException("El estado de admisión de destino no es válido.");
        if (!AllowedTransitions.TryGetValue(current, out var allowed) || !allowed.Contains(target))
            throw new InvalidOperationException($"El estado de admisión no puede transicionar de {current} a {target}.");
        if (target == AdmissionApplicationStatus.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Se requiere un motivo al rechazar una solicitud de admisión.");
        if (reason?.Trim().Length > 500)
            throw new ArgumentException("El motivo del estado de admisión no puede superar los 500 caracteres.");
    }
}
