using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.Application.Dtos;

public sealed class StudentDto
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserEmail { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int CareerId { get; set; }
    public string CareerName { get; set; } = null!;
    public string LegajoNumber { get; set; } = null!;
    public DateTime EnrollmentDate { get; set; }
    public string Status { get; set; } = null!;
    public int? CurrentStudyPlanId { get; set; }
    public string? CurrentStudyPlanName { get; set; }
}

public sealed class CreateStudentRequest
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int CareerId { get; set; }

    [Required]
    [MaxLength(50)]
    public string LegajoNumber { get; set; } = null!;

    public DateTime? EnrollmentDate { get; set; }
    public string Status { get; set; } = "Active";

    public int? StudyPlanId { get; set; }

    [MaxLength(500)]
    public string? StudyPlanMigrationReason { get; set; }
}
