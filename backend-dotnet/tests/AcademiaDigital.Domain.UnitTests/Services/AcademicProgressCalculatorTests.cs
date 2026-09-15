using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class AcademicProgressCalculatorTests
{
    private readonly AcademicProgressCalculator _calculator = new(new CourseEligibilityService());

    [Fact]
    public void Calculate_uses_mandatory_courses_and_latest_enrollment_status()
    {
        StudyPlanCourse[] courses =
        [
            Course(1),
            Course(2),
            Course(3),
            Course(4, isMandatory: false)
        ];

        Enrollment[] enrollments =
        [
            Enrollment(1, EnrollmentStatus.Failed, 2024, 2),
            Enrollment(1, EnrollmentStatus.Approved, 2025, 1),
            Enrollment(2, EnrollmentStatus.Regularized, 2025, 1),
            Enrollment(4, EnrollmentStatus.Promoted, 2025, 1)
        ];

        var result = _calculator.Calculate(courses, enrollments);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Approved);
        Assert.Equal(1, result.InProgress);
        Assert.Equal(1, result.Pending);
        Assert.Equal(33.33m, result.Percentage);
    }

    [Fact]
    public void Calculate_returns_zero_percentage_when_there_are_no_mandatory_courses()
    {
        var result = _calculator.Calculate(
            [Course(1, isMandatory: false)],
            [Enrollment(1, EnrollmentStatus.Approved, 2025, 1)]);

        Assert.Equal(0, result.Total);
        Assert.Equal(0, result.Approved);
        Assert.Equal(0, result.InProgress);
        Assert.Equal(0, result.Pending);
        Assert.Equal(0m, result.Percentage);
    }

    [Fact]
    public void Calculate_prefers_the_latest_semester_within_the_same_year()
    {
        var result = _calculator.Calculate(
            [Course(1)],
            [
                Enrollment(1, EnrollmentStatus.Approved, 2025, 1),
                Enrollment(1, EnrollmentStatus.Withdrawn, 2025, 2)
            ]);

        Assert.Equal(0, result.Approved);
        Assert.Equal(0, result.InProgress);
        Assert.Equal(1, result.Pending);
        Assert.Equal(0m, result.Percentage);
    }

    private static StudyPlanCourse Course(int courseId, bool isMandatory = true)
        => new() { CourseId = courseId, IsMandatory = isMandatory };

    private static Enrollment Enrollment(
        int courseId,
        EnrollmentStatus status,
        int academicYear,
        int semester)
        => new()
        {
            CourseId = courseId,
            Status = status,
            AcademicYear = academicYear,
            Semester = semester
        };
}
