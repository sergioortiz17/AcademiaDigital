namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Escuela (escuela) del instituto. Cada división se dicta en una escuela; la misma carrera puede tener
/// divisiones en varias escuelas. Las divisiones creadas antes de que existieran las escuelas quedan sin escuela.
/// </summary>
public class Campus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
