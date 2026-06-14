using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Students;

public sealed record GetEligibleCoursesForStudentQuery(long StudentId);
public sealed record GetStudentAcademicProgressQuery(long StudentId);
public sealed record AssignStudentStudyPlanCommand(long StudentId, AssignStudentStudyPlanRequest Request);

public sealed class GetEligibleCoursesForStudentQueryHandler(
    IStudentAcademicRepository studentAcademicRepository,
    CourseEligibilityService eligibilityService)
{
    public async Task<IReadOnlyList<EligibleCourseDto>> Handle(GetEligibleCoursesForStudentQuery query, CancellationToken ct = default)
    {
        var currentPlan = await studentAcademicRepository.GetCurrentStudyPlanAsync(query.StudentId, ct)
            ?? throw new KeyNotFoundException("Current study plan not found for student.");

        var courses = await studentAcademicRepository.GetStudyPlanCoursesAsync(currentPlan.StudyPlanId, ct);
        var enrollments = await studentAcademicRepository.GetEnrollmentsAsync(query.StudentId, ct);
        var prerequisites = await studentAcademicRepository.GetPrerequisitesAsync(currentPlan.StudyPlanId, ct);

        var enrollmentByCourse = enrollments
            .GroupBy(e => e.CourseId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.AcademicYear).ThenByDescending(e => e.Semester).First());

        var prerequisitesByCourse = prerequisites
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return courses.Select(course =>
        {
            enrollmentByCourse.TryGetValue(course.CourseId, out var enrollment);

            if (enrollment is not null && eligibilityService.IsApproved(enrollment.Status))
                return MapEligible(course, "AlreadyApproved", []);

            if (enrollment is not null && eligibilityService.IsInProgress(enrollment.Status))
                return MapEligible(course, "AlreadyEnrolled", []);

            var missing = prerequisitesByCourse.TryGetValue(course.CourseId, out var coursePrerequisites)
                ? coursePrerequisites
                    .Where(p => !eligibilityService.SatisfiesMinimumStatus(
                        enrollmentByCourse.TryGetValue(p.PrerequisiteCourseId, out var prerequisiteEnrollment)
                            ? prerequisiteEnrollment.Status
                            : null,
                        p.MinimumRequiredStatus))
                    .Select(p => MapMissing(p, enrollmentByCourse))
                    .ToList()
                : [];

            var hasStrictBlocker = missing.Any(p => p.PrerequisiteType == PrerequisiteType.Strict.ToString());
            var status = hasStrictBlocker
                ? "BlockedByStrictPrerequisite"
                : missing.Count > 0 ? "EligibleWithWarning" : "Eligible";

            return MapEligible(course, status, missing);
        }).ToList();
    }

    private static EligibleCourseDto MapEligible(StudyPlanCourse course, string status, IReadOnlyList<MissingPrerequisiteDto> missing) => new()
    {
        CourseId = course.CourseId,
        StudyPlanCourseId = course.Id,
        Code = course.Course.Code,
        Name = course.Course.Name,
        YearNumber = course.YearNumber,
        Semester = course.Semester,
        EligibilityStatus = status,
        MissingPrerequisites = missing
    };

    private static MissingPrerequisiteDto MapMissing(
        CoursePrerequisite prerequisite,
        IReadOnlyDictionary<int, Enrollment> enrollmentByCourse) => new()
    {
        CourseId = prerequisite.PrerequisiteCourseId,
        Code = prerequisite.PrerequisiteCourse.Code,
        Name = prerequisite.PrerequisiteCourse.Name,
        PrerequisiteType = prerequisite.PrerequisiteType.ToString(),
        RequiredStatus = prerequisite.MinimumRequiredStatus.ToString(),
        CurrentStatus = enrollmentByCourse.TryGetValue(prerequisite.PrerequisiteCourseId, out var enrollment)
            ? enrollment.Status.ToString()
            : null
    };
}

public sealed class GetStudentAcademicProgressQueryHandler(
    IStudentAcademicRepository studentAcademicRepository,
    AcademicProgressCalculator progressCalculator,
    CourseEligibilityService eligibilityService)
{
    public async Task<StudentAcademicProgressDto> Handle(GetStudentAcademicProgressQuery query, CancellationToken ct = default)
    {
        var currentPlan = await studentAcademicRepository.GetCurrentStudyPlanAsync(query.StudentId, ct)
            ?? throw new KeyNotFoundException("Current study plan not found for student.");

        var courses = await studentAcademicRepository.GetStudyPlanCoursesAsync(currentPlan.StudyPlanId, ct);
        var enrollments = await studentAcademicRepository.GetEnrollmentsAsync(query.StudentId, ct);
        var summary = progressCalculator.Calculate(courses, enrollments);

        var enrollmentByCourse = enrollments
            .GroupBy(e => e.CourseId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.AcademicYear).ThenByDescending(e => e.Semester).First());

        return new StudentAcademicProgressDto
        {
            StudentId = query.StudentId,
            CareerId = currentPlan.StudyPlan.CareerId,
            CareerName = currentPlan.StudyPlan.Career.Name,
            StudyPlanId = currentPlan.StudyPlanId,
            StudyPlanName = currentPlan.StudyPlan.Name,
            TotalCourses = summary.Total,
            ApprovedCourses = summary.Approved,
            InProgressCourses = summary.InProgress,
            PendingCourses = summary.Pending,
            ProgressPercentage = summary.Percentage,
            Courses = courses.Select(course =>
            {
                enrollmentByCourse.TryGetValue(course.CourseId, out var enrollment);
                return new StudentCourseProgressDto
                {
                    CourseId = course.CourseId,
                    Code = course.Course.Code,
                    Name = course.Course.Name,
                    YearNumber = course.YearNumber,
                    Semester = course.Semester,
                    AcademicStatus = enrollment is null
                        ? "Pending"
                        : eligibilityService.IsApproved(enrollment.Status)
                            ? "Approved"
                            : eligibilityService.IsInProgress(enrollment.Status) ? "InProgress" : enrollment.Status.ToString(),
                    FinalGrade = enrollment?.FinalGrade,
                    AcademicYear = enrollment?.AcademicYear
                };
            }).ToList()
        };
    }
}

public sealed class AssignStudentStudyPlanCommandHandler(
    IStudentRepository studentRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudentAcademicRepository studentAcademicRepository)
{
    public async Task Handle(AssignStudentStudyPlanCommand command, CancellationToken ct = default)
    {
        var student = await studentRepository.FindByIdAsync(command.StudentId, ct)
            ?? throw new KeyNotFoundException("Student not found.");

        var studyPlan = await studyPlanRepository.GetByIdAsync(command.Request.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Study plan not found.");

        if (studyPlan.CareerId != student.CareerId)
            throw new InvalidOperationException("Study plan must belong to the student career.");

        var assignment = new StudentStudyPlan
        {
            StudentId = command.StudentId,
            StudyPlanId = command.Request.StudyPlanId,
            MigrationReason = command.Request.MigrationReason
        };

        await studentAcademicRepository.AssignStudyPlanAsync(assignment, ct);
    }
}
