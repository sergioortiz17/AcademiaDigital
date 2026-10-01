using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ICourseSectionRepository
{
    Task<IReadOnlyList<CourseSection>> GetAllAsync(
        int? academicYear,
        int? semester,
        bool? isVacant,
        bool includeInactive,
        CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetByCourseAsync(int courseId, CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetByTeacherAsync(long teacherId, CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetByPeriodAsync(int year, int semester, CancellationToken ct = default);
    Task<IEnumerable<CourseSection>> GetVacantAsync(CancellationToken ct = default);
    Task<CourseSection?> FindByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Secciones ACTIVAS de una materia para un ciclo/cuatrimestre, SIN filtrar por división.
    /// Se usa en el auto-match de nivel 2 (Parte 6/14): la sección de una materia puntual se busca
    /// contra cualquier división, para que un recursante (que cursa una materia de otro año, fuera
    /// de su división) también quede vinculado. El término coincide por cuatrimestre, o por anual.
    /// El llamador decide: exactamente una = clara; cero o 2+ = ambiguo.
    /// </summary>
    Task<IReadOnlyList<CourseSection>> FindActiveByCourseTermAsync(
        int courseId, int academicYear, int semester, bool isAnnual, CancellationToken ct = default);

    Task<CourseSection> CreateAsync(CourseSection position, CancellationToken ct = default);
    Task<CourseSection> UpdateAsync(CourseSection position, CancellationToken ct = default);
    Task DeactivateAsync(CourseSection position, CancellationToken ct = default);
}
