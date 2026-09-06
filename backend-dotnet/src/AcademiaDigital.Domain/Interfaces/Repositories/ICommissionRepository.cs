using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICommissionRepository
{
    Task<Commission?> FindByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Devuelve todas las comisiones ACTIVAS que matchean Carrera + Año académico + Año del plan +
    /// Turno. Se devuelven todas (no una sola) para que el llamador decida: exactamente una =
    /// coincidencia clara; cero o más de una = ambiguo (no se asigna automáticamente).
    /// </summary>
    Task<IReadOnlyList<Commission>> FindMatchingActiveAsync(
        int careerId, int academicYear, int yearNumber, string shift, CancellationToken ct = default);
}
