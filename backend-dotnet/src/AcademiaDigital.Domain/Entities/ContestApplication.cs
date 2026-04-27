namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Postulación de un usuario a un concurso docente.
/// </summary>
public class ContestApplication
{
    public long Id { get; set; }
    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
    public string? Notes { get; set; }

    public int ContestId { get; set; }
    public TeacherContest Contest { get; set; } = null!;

    public long ApplicantId { get; set; }
    public User Applicant { get; set; } = null!;
}

public enum ApplicationStatus
{
    Pending = 0,
    UnderReview = 1,
    Approved = 2,
    Rejected = 3
}
