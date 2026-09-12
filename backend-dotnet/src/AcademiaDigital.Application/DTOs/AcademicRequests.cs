using System.ComponentModel.DataAnnotations;

namespace AcademiaDigital.Application.Dtos;

public sealed class CreateStudyPlanRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int VersionNumber { get; set; }

    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

public sealed class UpsertCourseRequest
{
    [Required]
    [MaxLength(20)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class AddCourseToStudyPlanRequest
{
    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue)]
    public int YearNumber { get; set; }

    [Range(1, 2)]
    public int Semester { get; set; }

    public int? CourseTypeId { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int SortOrder { get; set; }
    public decimal? Credits { get; set; }
    public int? WorkloadHours { get; set; }
    public CourseApprovalRuleRequest? ApprovalRule { get; set; }
}

public sealed class CourseApprovalRuleRequest
{
    public decimal? MinimumRegularGrade { get; set; }
    public decimal? MinimumPromotionGrade { get; set; }
    public decimal MinimumFinalExamGrade { get; set; } = 6m;
    public decimal? MinimumAttendancePercentage { get; set; }
    public bool RequiresFinalExam { get; set; } = true;
    public bool AllowsPromotion { get; set; }
    public string? PolicyJson { get; set; }
}

public sealed class AddCoursePrerequisiteRequest
{
    [Range(1, int.MaxValue)]
    public int PrerequisiteCourseId { get; set; }

    public string PrerequisiteType { get; set; } = "Strict";
    public string MinimumRequiredStatus { get; set; } = "Approved";
}

public sealed class AssignStudentStudyPlanRequest
{
    [Range(1, int.MaxValue)]
    public int StudyPlanId { get; set; }

    [MaxLength(500)]
    public string? MigrationReason { get; set; }
}
