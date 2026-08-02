namespace AcademiaDigital.Domain.Entities;

public class AcademicEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public string EventType { get; set; } = "Otro"; // Examen, EntregaTP, Clase, Otro
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
