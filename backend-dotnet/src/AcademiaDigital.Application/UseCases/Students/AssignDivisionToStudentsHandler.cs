using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Students;

/// <summary>
/// Asigna varios alumnos a una misma división en un solo pedido (pantalla "Asignar división a
/// alumno"). Para cada alumno toma su plan de estudios vigente en la carrera de la división y
/// reutiliza la asignación individual (<see cref="IStudentManagementService.AssignAcademicAsync"/>),
/// con su misma validación y transacción. Un alumno que no puede asignarse no frena al resto:
/// el resultado informa los asignados y el motivo de cada uno que falló.
/// </summary>
public sealed class AssignDivisionToStudentsHandler(
    IDivisionRepository divisions,
    IStudentAcademicRepository academic,
    IStudentManagementService students)
{
    public const int MaxPerRequest = 300;

    public async Task<BulkDivisionAssignmentResult> Handle(
        int careerId, int divisionId, IReadOnlyCollection<long> studentIds, string? reason, long actorId,
        CancellationToken ct = default)
    {
        var ids = studentIds.Where(id => id > 0).Distinct().ToArray();
        if (ids.Length == 0)
            throw new ArgumentException("Selecciona al menos un alumno.");
        if (ids.Length > MaxPerRequest)
            throw new ArgumentException($"Se pueden asignar hasta {MaxPerRequest} alumnos por vez.");

        var division = await divisions.FindByIdAsync(divisionId, ct);
        if (division is null || division.CareerId != careerId)
            throw new KeyNotFoundException("División no encontrada en esta carrera.");
        if (!division.IsActive)
            throw new InvalidOperationException("La división está inactiva.");

        var assigned = new List<long>();
        var failed = new List<BulkAssignmentFailure>();
        foreach (var studentId in ids)
        {
            var plan = await academic.GetCurrentStudyPlanAsync(studentId, division.CareerId, ct);
            if (plan is null)
            {
                failed.Add(new BulkAssignmentFailure(studentId, "No tiene un plan de estudios vigente en la carrera."));
                continue;
            }
            try
            {
                await students.AssignAcademicAsync(studentId, new CreateAcademicAssignmentRequest(
                    division.CareerId, plan.StudyPlanId, division.Id, division.AcademicYear, division.YearNumber,
                    reason), actorId, ct);
                assigned.Add(studentId);
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
            {
                failed.Add(new BulkAssignmentFailure(studentId, ex.Message));
            }
        }

        return new BulkDivisionAssignmentResult(assigned, failed);
    }
}

public sealed record BulkAssignmentFailure(long StudentId, string Reason);
public sealed record BulkDivisionAssignmentResult(IReadOnlyList<long> Assigned, IReadOnlyList<BulkAssignmentFailure> Failed);
