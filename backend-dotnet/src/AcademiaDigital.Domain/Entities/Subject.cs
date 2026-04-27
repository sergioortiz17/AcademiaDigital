namespace AcademiaDigital.Domain.Entities;

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public int WeeklyHours { get; set; }
    public int Year { get; set; }
    public int Semester { get; set; }
    public bool IsActive { get; set; } = true;

    public int CareerId { get; set; }
    public Career Career { get; set; } = null!;

    // Correlativas: materias que deben aprobarse antes de cursar esta
    public ICollection<SubjectPrerequisite> Prerequisites { get; set; } = [];

    // Materias para las cuales esta es correlativa
    public ICollection<SubjectPrerequisite> IsPrerequisiteFor { get; set; } = [];
}
