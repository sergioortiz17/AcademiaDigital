namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Sede (escuela) del instituto. Cada división se dicta en una sede; la misma carrera puede tener
/// divisiones en varias sedes. Las divisiones creadas antes de que existieran las sedes quedan sin sede.
/// </summary>
public class Campus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
