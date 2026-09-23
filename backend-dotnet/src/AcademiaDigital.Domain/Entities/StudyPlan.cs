using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Plan de estudios de una carrera. Modelo de dominio RICO: tiene una máquina de estados real
/// (Draft → Active → Archived) que antes se mutaba con <c>plan.Status = ...</c> desde cualquier
/// lado sin proteger la transición. Ahora las transiciones pasan por <see cref="Activate"/> /
/// <see cref="Archive"/>, que validan.
///
/// Nota: la invariante "un solo plan Active por carrera" se sigue orquestando en el handler que
/// activa (necesita ver los planes hermanos de la carrera); acá se protege que CADA transición
/// individual sea válida. Los setters son privados; EF materializa por backing field.
/// </summary>
public class StudyPlan
{
    public int Id { get; private set; }
    public int CareerId { get; private set; }
    public Career Career { get; private set; } = null!;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int VersionNumber { get; private set; }
    public StudyPlanStatus Status { get; private set; } = StudyPlanStatus.Draft;
    public DateOnly? EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; private set; } = [];

    public ICollection<StudyPlanCourse> Courses { get; private set; } = [];
    public ICollection<CoursePrerequisite> Prerequisites { get; private set; } = [];
    public ICollection<StudentStudyPlan> StudentStudyPlans { get; private set; } = [];

    private StudyPlan() { } // EF

    /// <summary>Crea un plan en estado Draft (todo plan nace como borrador).</summary>
    public static StudyPlan Create(
        int careerId, string code, string name, int versionNumber,
        DateOnly? effectiveFrom = null, DateOnly? effectiveTo = null)
    {
        var plan = new StudyPlan
        {
            CareerId = careerId,
            Status = StudyPlanStatus.Draft,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        plan.ApplyDetails(code, name, versionNumber, effectiveFrom, effectiveTo);
        return plan;
    }

    public void Update(string code, string name, int versionNumber, DateOnly? effectiveFrom, DateOnly? effectiveTo)
    {
        ApplyDetails(code, name, versionNumber, effectiveFrom, effectiveTo);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Activa este plan (Draft/Archived → Active). Idempotente si ya está activo.</summary>
    public void Activate()
    {
        Status = StudyPlanStatus.Active;
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Archiva este plan (deja de ser el vigente). Idempotente si ya está archivado.</summary>
    public void Archive()
    {
        Status = StudyPlanStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ApplyDetails(string code, string name, int versionNumber, DateOnly? effectiveFrom, DateOnly? effectiveTo)
    {
        code = (code ?? string.Empty).Trim();
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código del plan es obligatorio.", nameof(code));
        if (code.Length > 20)
            throw new ArgumentException("El código del plan no puede superar los 20 caracteres.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del plan es obligatorio.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("El nombre del plan no puede superar los 200 caracteres.", nameof(name));
        if (versionNumber < 1)
            throw new ArgumentException("El número de versión debe ser al menos 1.", nameof(versionNumber));
        if (effectiveFrom.HasValue && effectiveTo.HasValue && effectiveTo.Value < effectiveFrom.Value)
            throw new ArgumentException("La vigencia 'hasta' no puede ser anterior a 'desde'.", nameof(effectiveTo));

        Code = code;
        Name = name;
        VersionNumber = versionNumber;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }
}
