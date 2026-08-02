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
    public IReadOnlyList<StudentCareerDto> Careers { get; set; } = [];
}

public sealed record StudentCareerDto(long Id, int CareerId, string CareerName, DateTime EnrollmentDate,
    bool IsActive, bool IsPrimary, int? CurrentStudyPlanId, string? CurrentStudyPlanName);

public sealed record AddStudentCareerRequest(
    [Range(1, int.MaxValue)] int CareerId,
    DateTime? EnrollmentDate);

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
    public string Status { get; set; } = "Regular";

    public int? StudyPlanId { get; set; }

    [MaxLength(500)]
    public string? StudyPlanMigrationReason { get; set; }

    public int? CommissionId { get; set; }
    [Range(2000, 2100)] public int? AcademicYear { get; set; }
    [Range(1, 20)] public int? YearNumber { get; set; }
    [MaxLength(300)] public string? AddressLine { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(100)] public string? Province { get; set; }
    [MaxLength(20)] public string? PostalCode { get; set; }
    [MaxLength(200)] public string? EmergencyContactName { get; set; }
    [MaxLength(100)] public string? EmergencyContactRelationship { get; set; }
    [MaxLength(30)] public string? EmergencyContactPhone { get; set; }
}
