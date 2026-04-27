namespace AcademiaDigital.Domain.Entities;

/// <summary>
/// Representa una correlativa: PrerequisiteSubject debe aprobarse antes de cursar Subject.
/// </summary>
public class SubjectPrerequisite
{
    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public int PrerequisiteSubjectId { get; set; }
    public Subject PrerequisiteSubject { get; set; } = null!;
}
