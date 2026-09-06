namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Carrera. Modelo de dominio RICO (patrón de <c>Receipt.MarkReady</c>): las propiedades tienen
/// setters privados y solo se crean/modifican a través de <see cref="Create"/> / <see cref="Update"/>,
/// que validan las invariantes. Así es imposible dejar la entidad en un estado inválido
/// (p. ej. <c>TotalCredits = -50</c> o un código vacío) desde cualquier parte del código.
///
/// EF Core materializa igual estas entidades (usa los backing fields), así que los setters privados
/// no afectan la persistencia. El constructor sin parámetros es solo para EF.
/// </summary>
public class Career
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int TotalCredits { get; private set; }
    public int DurationYears { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; private set; } = [];

    public ICollection<Course> Courses { get; private set; } = [];
    public ICollection<StudyPlan> StudyPlans { get; private set; } = [];
    public ICollection<Student> Students { get; private set; } = [];
    public ICollection<StudentCareer> StudentCareers { get; private set; } = [];

    private Career() { } // EF

    public static Career Create(string name, string code, string? description, int durationYears, int totalCredits = 0)
    {
        var career = new Career { CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, IsActive = true };
        career.Apply(name, code, description, durationYears, totalCredits);
        return career;
    }

    public void Update(string name, string code, string? description, int durationYears, int totalCredits)
    {
        Apply(name, code, description, durationYears, totalCredits);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private void Apply(string name, string code, string? description, int durationYears, int totalCredits)
    {
        name = (name ?? string.Empty).Trim();
        code = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre de la carrera es obligatorio.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("El nombre de la carrera no puede superar los 200 caracteres.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código de la carrera es obligatorio.", nameof(code));
        if (code.Length > 20)
            throw new ArgumentException("El código de la carrera no puede superar los 20 caracteres.", nameof(code));
        if (durationYears < 1)
            throw new ArgumentException("La duración en años debe ser al menos 1.", nameof(durationYears));
        if (totalCredits < 0)
            throw new ArgumentException("Los créditos totales no pueden ser negativos.", nameof(totalCredits));

        Name = name;
        Code = code;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        DurationYears = durationYears;
        TotalCredits = totalCredits;
    }
}
