namespace AcademiaDigital.Application.Services;

public sealed record BulkFailure(long Id, string Reason);
public sealed record BulkGraduateResult(int Graduated, IReadOnlyList<BulkFailure> Failed);
public sealed record BulkEnrollResult(int Students, int Created, int AlreadyEnrolled, IReadOnlyList<string> Courses);

/// <summary>Actos administrativos de a muchos alumnos (egreso e inscripción al cursado).</summary>
public interface IBulkStudentActionsService
{
    /// <summary>Marca egresados con la fecha de egreso y deja el cambio en el historial de estados.</summary>
    Task<BulkGraduateResult> GraduateAsync(IReadOnlyCollection<long> studentIds, DateOnly graduatedOn, string reason, long actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Inscribe a todos los alumnos asignados al curso en las materias del plan de su año
    /// para el cuatrimestre indicado (las anuales van con el 1.º). Saltea las inscripciones que ya existen.
    /// </summary>
    Task<BulkEnrollResult> EnrollDivisionAsync(int divisionId, int semester, CancellationToken ct = default);
}
