namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Concurso docente para cubrir cargos/plazas en la institución.
/// </summary>
public class TeacherContest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime OpenDate { get; set; }
    public DateTime CloseDate { get; set; }
    public ContestStatus Status { get; set; } = ContestStatus.Draft;

    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    public int? CareerId { get; set; }
    public Career? Career { get; set; }

    public ICollection<ContestApplication> Applications { get; set; } = [];
}

public enum ContestStatus
{
    Draft = 0,
    Open = 1,
    InReview = 2,
    Closed = 3,
    Resolved = 4
}
