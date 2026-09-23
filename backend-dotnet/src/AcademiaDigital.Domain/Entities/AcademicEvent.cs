using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Entities;

public class AcademicEvent
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public string EventType { get; set; } = "Otro"; // Examen, EntregaTP, Clase, Feriado, Otro
    public EventScope Scope { get; set; } = EventScope.Global;
    public string? Modality { get; set; } // Presencial, Virtual
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public long CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public int? CourseSectionId { get; set; }
    public CourseSection? CourseSection { get; set; }
}
