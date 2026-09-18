namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Vínculo explícito profesor ↔ carrera (simetría con StudentCareer). Responde directo a
/// "¿en qué carreras dicta este profesor?" sin derivarlo cada vez de sus asignaciones. Se
/// auto-crea (idempotente, no bloqueante) cuando se asigna al profesor a una CourseSection de una
/// carrera con la que todavía no tenía vínculo. Todavía no se usa como restricción de negocio.
/// </summary>
public class TeacherCareer
{
    public long Id { get; set; }
    public long TeacherId { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}
