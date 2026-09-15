namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Auditoría de cambios de estado de una inscripción hechos por vía administrativa (fuera del
/// circuito formal de planilla + mesa). Mismo patrón que StudentStatusHistory: registra estado
/// previo/nuevo, la nota final aplicada, el motivo obligatorio, quién y cuándo.
/// </summary>
public class EnrollmentStatusHistory
{
    public long Id { get; set; }
    public long EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;
    public EnrollmentStatus PreviousStatus { get; set; }
    public EnrollmentStatus NewStatus { get; set; }
    public decimal? PreviousFinalGrade { get; set; }
    public decimal? NewFinalGrade { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public long ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;
}
