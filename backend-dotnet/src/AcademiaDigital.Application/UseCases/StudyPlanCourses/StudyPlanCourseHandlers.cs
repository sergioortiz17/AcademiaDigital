using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.StudyPlanCourses;

public sealed record GetStudyPlanCoursesQuery(int StudyPlanId);
public sealed record AddCourseToStudyPlanCommand(int StudyPlanId, AddCourseToStudyPlanRequest Request);
public sealed record UpdateStudyPlanCourseCommand(int StudyPlanId, int StudyPlanCourseId, AddCourseToStudyPlanRequest Request);
public sealed record RemoveCourseFromStudyPlanCommand(int StudyPlanId, int StudyPlanCourseId);
public sealed record SetCourseApprovalRuleCommand(int StudyPlanId, int StudyPlanCourseId, CourseApprovalRuleRequest Rule);

public sealed class GetStudyPlanCoursesQueryHandler(IStudyPlanCourseRepository studyPlanCourseRepository)
{
    public async Task<IReadOnlyList<StudyPlanCourseDetailDto>> Handle(GetStudyPlanCoursesQuery query, CancellationToken ct = default)
    {
        var courses = await studyPlanCourseRepository.GetByStudyPlanIdAsync(query.StudyPlanId, ct);
        return courses.Select(Map).ToList();
    }

    private static StudyPlanCourseDetailDto Map(StudyPlanCourse course) => new()
    {
        Id = course.Id,
        StudyPlanId = course.StudyPlanId,
        CourseId = course.CourseId,
        CourseCode = course.Course.Code,
        CourseName = course.Course.Name,
        YearNumber = course.YearNumber,
        Semester = course.Semester,
        IsAnnual = course.IsAnnual,
        SortOrder = course.SortOrder,
        IsMandatory = course.IsMandatory,
        Credits = course.Credits,
        WorkloadHours = course.WorkloadHours,
        CourseType = course.CourseType?.Name,
        ApprovalRule = course.ApprovalRule is null ? null : new CourseApprovalRuleDto
        {
            MinimumRegularGrade = course.ApprovalRule.MinimumRegularGrade,
            MinimumPromotionGrade = course.ApprovalRule.MinimumPromotionGrade,
            MinimumFinalExamGrade = course.ApprovalRule.MinimumFinalExamGrade,
            MinimumAttendancePercentage = course.ApprovalRule.MinimumAttendancePercentage,
            RequiresFinalExam = course.ApprovalRule.RequiresFinalExam,
            AllowsPromotion = course.ApprovalRule.AllowsPromotion
        }
    };
}

public sealed class AddCourseToStudyPlanCommandHandler(
    IStudyPlanRepository studyPlanRepository,
    ICourseRepository courseRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository)
{
    public async Task<StudyPlanCourseDetailDto> Handle(AddCourseToStudyPlanCommand command, CancellationToken ct = default)
    {
        var studyPlan = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Study plan not found.");

        var existsInCareer = await courseRepository.ExistsInCareerAsync(command.Request.CourseId, studyPlan.CareerId, ct);
        if (!existsInCareer) throw new InvalidOperationException("Course does not belong to the study plan career.");

        var exists = await studyPlanCourseRepository.ExistsAsync(studyPlan.Id, command.Request.CourseId, ct);
        if (exists) throw new InvalidOperationException("Course already exists in this study plan.");

        var studyPlanCourse = new StudyPlanCourse
        {
            StudyPlanId = studyPlan.Id,
            CourseId = command.Request.CourseId,
            YearNumber = command.Request.YearNumber,
            Semester = command.Request.Semester,
            CourseTypeId = command.Request.CourseTypeId,
            SortOrder = command.Request.SortOrder,
            IsMandatory = command.Request.IsMandatory,
            Credits = command.Request.Credits,
            WorkloadHours = command.Request.WorkloadHours,
            ApprovalRule = command.Request.ApprovalRule is null ? null : new CourseApprovalRule
            {
                MinimumRegularGrade = command.Request.ApprovalRule.MinimumRegularGrade,
                MinimumPromotionGrade = command.Request.ApprovalRule.MinimumPromotionGrade,
                MinimumFinalExamGrade = command.Request.ApprovalRule.MinimumFinalExamGrade,
                MinimumAttendancePercentage = command.Request.ApprovalRule.MinimumAttendancePercentage,
                RequiresFinalExam = command.Request.ApprovalRule.RequiresFinalExam,
                AllowsPromotion = command.Request.ApprovalRule.AllowsPromotion,
                PolicyJson = command.Request.ApprovalRule.PolicyJson
            }
        };

        var created = await studyPlanCourseRepository.CreateAsync(studyPlanCourse, ct);
        var hydrated = await studyPlanCourseRepository.GetByIdAsync(created.Id, ct) ?? created;
        return Map(hydrated);
    }

    private static StudyPlanCourseDetailDto Map(StudyPlanCourse course) => new()
    {
        Id = course.Id,
        StudyPlanId = course.StudyPlanId,
        CourseId = course.CourseId,
        CourseCode = course.Course.Code,
        CourseName = course.Course.Name,
        YearNumber = course.YearNumber,
        Semester = course.Semester,
        IsAnnual = course.IsAnnual,
        SortOrder = course.SortOrder,
        IsMandatory = course.IsMandatory,
        Credits = course.Credits,
        WorkloadHours = course.WorkloadHours,
        CourseType = course.CourseType?.Name,
        ApprovalRule = course.ApprovalRule is null ? null : new CourseApprovalRuleDto
        {
            MinimumRegularGrade = course.ApprovalRule.MinimumRegularGrade,
            MinimumPromotionGrade = course.ApprovalRule.MinimumPromotionGrade,
            MinimumFinalExamGrade = course.ApprovalRule.MinimumFinalExamGrade,
            MinimumAttendancePercentage = course.ApprovalRule.MinimumAttendancePercentage,
            RequiresFinalExam = course.ApprovalRule.RequiresFinalExam,
            AllowsPromotion = course.ApprovalRule.AllowsPromotion
        }
    };
}

public sealed class UpdateStudyPlanCourseCommandHandler(IStudyPlanCourseRepository studyPlanCourseRepository)
{
    public async Task Handle(UpdateStudyPlanCourseCommand command, CancellationToken ct = default)
    {
        var studyPlanCourse = await studyPlanCourseRepository.GetByIdAsync(command.StudyPlanCourseId, ct)
            ?? throw new KeyNotFoundException("Study plan course not found.");

        if (studyPlanCourse.StudyPlanId != command.StudyPlanId) throw new KeyNotFoundException("Study plan course not found.");

        studyPlanCourse.YearNumber = command.Request.YearNumber;
        studyPlanCourse.Semester = command.Request.Semester;
        studyPlanCourse.CourseTypeId = command.Request.CourseTypeId;
        studyPlanCourse.SortOrder = command.Request.SortOrder;
        studyPlanCourse.IsMandatory = command.Request.IsMandatory;
        studyPlanCourse.Credits = command.Request.Credits;
        studyPlanCourse.WorkloadHours = command.Request.WorkloadHours;
        studyPlanCourse.UpdatedAt = DateTime.UtcNow;

        await studyPlanCourseRepository.UpdateAsync(studyPlanCourse, ct);
    }
}

public sealed class RemoveCourseFromStudyPlanCommandHandler(IStudyPlanCourseRepository studyPlanCourseRepository)
{
    public async Task Handle(RemoveCourseFromStudyPlanCommand command, CancellationToken ct = default)
    {
        var studyPlanCourse = await studyPlanCourseRepository.GetByIdAsync(command.StudyPlanCourseId, ct)
            ?? throw new KeyNotFoundException("Study plan course not found.");

        if (studyPlanCourse.StudyPlanId != command.StudyPlanId) throw new KeyNotFoundException("Study plan course not found.");

        await studyPlanCourseRepository.DeleteAsync(studyPlanCourse, ct);
    }
}

/// <summary>
/// Upsert de la regla de aprobación (CourseApprovalRule) de una materia del plan. Permite
/// configurar/editar de forma persistente AllowsPromotion, mínimos y RequiresFinalExam sin SQL.
/// </summary>
public sealed class SetCourseApprovalRuleCommandHandler(IStudyPlanCourseRepository studyPlanCourseRepository)
{
    public async Task Handle(SetCourseApprovalRuleCommand command, CancellationToken ct = default)
    {
        var rule = command.Rule;

        // Validaciones de negocio simples y coherentes con la escala 1..10.
        if (rule.MinimumFinalExamGrade < 1m || rule.MinimumFinalExamGrade > 10m)
            throw new ArgumentException("La nota mínima de mesa final debe estar entre 1 y 10.");
        if (rule.MinimumRegularGrade is { } mr && (mr < 1m || mr > 10m))
            throw new ArgumentException("La nota mínima para regularizar debe estar entre 1 y 10.");
        if (rule.MinimumPromotionGrade is { } mp && (mp < 1m || mp > 10m))
            throw new ArgumentException("La nota mínima de promoción debe estar entre 1 y 10.");
        if (rule.AllowsPromotion && rule.MinimumPromotionGrade is null)
            throw new ArgumentException("Si la materia permite promoción, se requiere la nota mínima de promoción.");
        if (rule.MinimumRegularGrade is { } r && rule.MinimumPromotionGrade is { } p && p < r)
            throw new ArgumentException("La nota mínima de promoción no puede ser menor que la de regularización.");

        await studyPlanCourseRepository.SetApprovalRuleAsync(
            command.StudyPlanId, command.StudyPlanCourseId, rule.MinimumRegularGrade, rule.MinimumPromotionGrade,
            rule.MinimumFinalExamGrade, rule.MinimumAttendancePercentage, rule.RequiresFinalExam, rule.AllowsPromotion, ct);
    }
}
