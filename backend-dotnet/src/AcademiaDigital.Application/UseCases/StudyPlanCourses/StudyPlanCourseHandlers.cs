using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.StudyPlanCourses;

public sealed record GetStudyPlanCoursesQuery(int StudyPlanId);
public sealed record AddCourseToStudyPlanCommand(int StudyPlanId, AddCourseToStudyPlanRequest Request);
public sealed record UpdateStudyPlanCourseCommand(int StudyPlanId, int StudyPlanCourseId, AddCourseToStudyPlanRequest Request);
public sealed record RemoveCourseFromStudyPlanCommand(int StudyPlanId, int StudyPlanCourseId);

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
        SortOrder = course.SortOrder,
        IsMandatory = course.IsMandatory,
        Credits = course.Credits,
        WorkloadHours = course.WorkloadHours,
        CourseType = course.CourseType?.Name
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
        SortOrder = course.SortOrder,
        IsMandatory = course.IsMandatory,
        Credits = course.Credits,
        WorkloadHours = course.WorkloadHours,
        CourseType = course.CourseType?.Name
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
