using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Services;

public sealed class EnrollmentEligibilityPolicy(CourseEligibilityService courseEligibilityService)
{
    public void EnsureCanEnroll(
        IReadOnlyCollection<StudyPlanCourse> selectedCourses,
        IReadOnlyCollection<CoursePrerequisite> prerequisites,
        IReadOnlyCollection<Enrollment> enrollmentHistory)
    {
        var latestEnrollmentByCourse = enrollmentHistory
            .GroupBy(enrollment => enrollment.CourseId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(enrollment => enrollment.AcademicYear)
                    .ThenByDescending(enrollment => enrollment.Semester)
                    .ThenByDescending(enrollment => enrollment.EnrollmentDate)
                    .First());

        var selectedCourseIds = selectedCourses.Select(course => course.CourseId).ToHashSet();
        var unavailableCourses = selectedCourseIds
            .Where(courseId => latestEnrollmentByCourse.TryGetValue(courseId, out var enrollment)
                && (courseEligibilityService.IsApproved(enrollment.Status)
                    || courseEligibilityService.IsInProgress(enrollment.Status)))
            .OrderBy(courseId => courseId)
            .ToArray();
        if (unavailableCourses.Length > 0)
            throw new InvalidOperationException(
                $"Courses already approved or in progress cannot be enrolled again: {string.Join(", ", unavailableCourses)}.");

        var missingStrictPrerequisites = prerequisites
            .Where(prerequisite => prerequisite.IsActive
                && prerequisite.PrerequisiteType == PrerequisiteType.Strict
                && selectedCourseIds.Contains(prerequisite.CourseId))
            .Where(prerequisite => !courseEligibilityService.SatisfiesMinimumStatus(
                latestEnrollmentByCourse.TryGetValue(prerequisite.PrerequisiteCourseId, out var enrollment)
                    ? enrollment.Status
                    : null,
                prerequisite.MinimumRequiredStatus))
            .OrderBy(prerequisite => prerequisite.CourseId)
            .ThenBy(prerequisite => prerequisite.PrerequisiteCourseId)
            .Select(prerequisite => $"{prerequisite.CourseId}->{prerequisite.PrerequisiteCourseId} ({prerequisite.MinimumRequiredStatus})")
            .ToArray();
        if (missingStrictPrerequisites.Length > 0)
            throw new InvalidOperationException(
                $"Strict prerequisites are not satisfied: {string.Join(", ", missingStrictPrerequisites)}.");
    }
}
