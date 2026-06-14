using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public class AcademicProgressCalculator(CourseEligibilityService eligibilityService)
{
    public (int Total, int Approved, int InProgress, int Pending, decimal Percentage) Calculate(
        IReadOnlyCollection<StudyPlanCourse> courses,
        IReadOnlyCollection<Enrollment> enrollments)
    {
        var mandatoryCourses = courses.Where(c => c.IsMandatory).ToList();
        var enrollmentByCourse = enrollments
            .GroupBy(e => e.CourseId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.AcademicYear).ThenByDescending(e => e.Semester).First());

        var total = mandatoryCourses.Count;
        var approved = mandatoryCourses.Count(c =>
            enrollmentByCourse.TryGetValue(c.CourseId, out var enrollment) && eligibilityService.IsApproved(enrollment.Status));
        var inProgress = mandatoryCourses.Count(c =>
            enrollmentByCourse.TryGetValue(c.CourseId, out var enrollment) && eligibilityService.IsInProgress(enrollment.Status));
        var pending = Math.Max(0, total - approved - inProgress);
        var percentage = total == 0 ? 0 : Math.Round(approved * 100m / total, 2);

        return (total, approved, inProgress, pending, percentage);
    }
}
