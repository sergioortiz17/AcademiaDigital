using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Enrollments;

/// <summary>
/// Atajo administrativo para marcar una materia (inscripción) como aprobada por fuera del circuito
/// formal de planilla + mesa de examen. Fija el estado y la nota final, y registra auditoría.
/// </summary>
public sealed record AdminApproveEnrollmentCommand(
    long EnrollmentId,
    decimal FinalGrade,
    string? Reason,
    long ActorUserId,
    bool Promote = false);

public sealed record AdminApproveEnrollmentResult(long EnrollmentId, EnrollmentStatus Status, decimal? FinalGrade);

public sealed class AdminApproveEnrollmentCommandHandler(
    IEnrollmentRepository enrollmentRepository,
    TimeProvider timeProvider)
{
    public async Task<AdminApproveEnrollmentResult> Handle(AdminApproveEnrollmentCommand command, CancellationToken ct = default)
    {
        // Escala de calificación 1..10 (aprobado a partir de 4, pero eso lo valida el negocio aparte;
        // acá sólo garantizamos un rango sano para el atajo administrativo).
        if (command.FinalGrade < 1m || command.FinalGrade > 10m)
            throw new ArgumentException("La nota final debe estar entre 1 y 10.", nameof(command.FinalGrade));

        // El motivo es opcional: si no se da, queda auditado igual (con Reason vacío).
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? string.Empty : command.Reason.Trim();
        var newStatus = command.Promote ? EnrollmentStatus.Promoted : EnrollmentStatus.Approved;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var updated = await enrollmentRepository.AdminApproveAsync(
            command.EnrollmentId, newStatus, command.FinalGrade, reason, command.ActorUserId, now, ct);

        return new AdminApproveEnrollmentResult(updated.Id, updated.Status, updated.FinalGrade);
    }
}
