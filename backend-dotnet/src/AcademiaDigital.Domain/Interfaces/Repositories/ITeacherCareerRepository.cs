using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Interfaces.Repositories;

public interface ITeacherCareerRepository
{
    /// <summary>
    /// Garantiza (idempotente) que exista el vínculo profesor↔carrera. Si ya existe no hace nada;
    /// si no, lo crea. Usa el DbContext compartido para participar de la transacción en curso.
    /// No bloquea: solo captura el dato.
    /// </summary>
    Task EnsureAsync(long teacherId, int careerId, CancellationToken ct = default);

    Task<IReadOnlyList<TeacherCareer>> GetByTeacherAsync(long teacherId, CancellationToken ct = default);
}
