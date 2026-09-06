using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Correlatividad: una materia (Course) requiere otra (PrerequisiteCourse) dentro de un plan.
/// Modelo de dominio RICO: se crea vía <see cref="Create"/>, que protege la invariante de negocio
/// "una materia no puede ser correlativa de sí misma". Setters privados; EF materializa por
/// backing field.
/// </summary>
public class CoursePrerequisite
{
    public int Id { get; private set; }
    public int StudyPlanId { get; private set; }
    public StudyPlan StudyPlan { get; private set; } = null!;
    public int CourseId { get; private set; }
    public Course Course { get; private set; } = null!;
    public int PrerequisiteCourseId { get; private set; }
    public Course PrerequisiteCourse { get; private set; } = null!;
    public PrerequisiteType PrerequisiteType { get; private set; } = PrerequisiteType.Strict;
    public MinimumRequiredStatus MinimumRequiredStatus { get; private set; } = MinimumRequiredStatus.Approved;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private CoursePrerequisite() { } // EF

    /// <summary>
    /// Crea una correlatividad persistible. Valida que la materia no sea correlativa de sí misma.
    /// </summary>
    public static CoursePrerequisite Create(
        int studyPlanId,
        int courseId,
        int prerequisiteCourseId,
        PrerequisiteType prerequisiteType = PrerequisiteType.Strict,
        MinimumRequiredStatus minimumRequiredStatus = MinimumRequiredStatus.Approved)
    {
        if (courseId == prerequisiteCourseId)
            throw new ArgumentException("Una materia no puede ser correlativa de sí misma.", nameof(prerequisiteCourseId));

        return new CoursePrerequisite
        {
            StudyPlanId = studyPlanId,
            CourseId = courseId,
            PrerequisiteCourseId = prerequisiteCourseId,
            PrerequisiteType = prerequisiteType,
            MinimumRequiredStatus = minimumRequiredStatus,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Crea un "borde" liviano (solo course/prerequisite ids, activo) para usar EN MEMORIA con
    /// <c>PrerequisiteCycleValidator</c> durante la validación de un import — no se persiste.
    /// Permite reusar el detector de ciclos sin exponer setters públicos ni persistir ids-índice.
    /// </summary>
    public static CoursePrerequisite Edge(int courseId, int prerequisiteCourseId) => new()
    {
        CourseId = courseId,
        PrerequisiteCourseId = prerequisiteCourseId,
        IsActive = true
    };

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
