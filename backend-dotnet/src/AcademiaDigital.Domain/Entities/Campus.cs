namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Escuela (sede) del instituto. Cada curso se dicta en una escuela; la misma carrera puede tener
/// cursos en varias escuelas. Los cursos creados antes de que existieran las escuelas quedan sin escuela.
/// </summary>
public class Campus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
