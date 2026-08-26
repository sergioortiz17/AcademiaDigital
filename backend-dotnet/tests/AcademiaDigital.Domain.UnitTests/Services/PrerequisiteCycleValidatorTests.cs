using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class PrerequisiteCycleValidatorTests
{
    private readonly PrerequisiteCycleValidator _validator = new();

    [Fact]
    public void WouldCreateCycle_detects_a_direct_cycle()
    {
        CoursePrerequisite[] existing = [Prerequisite(courseId: 2, prerequisiteCourseId: 1)];

        Assert.True(_validator.WouldCreateCycle(existing, courseId: 1, prerequisiteCourseId: 2));
    }

    [Fact]
    public void WouldCreateCycle_detects_a_transitive_cycle()
    {
        CoursePrerequisite[] existing =
        [
            Prerequisite(courseId: 2, prerequisiteCourseId: 3),
            Prerequisite(courseId: 3, prerequisiteCourseId: 1)
        ];

        Assert.True(_validator.WouldCreateCycle(existing, courseId: 1, prerequisiteCourseId: 2));
    }

    [Fact]
    public void WouldCreateCycle_ignores_inactive_prerequisites()
    {
        CoursePrerequisite[] existing =
        [
            Prerequisite(courseId: 2, prerequisiteCourseId: 1, isActive: false)
        ];

        Assert.False(_validator.WouldCreateCycle(existing, courseId: 1, prerequisiteCourseId: 2));
    }

    [Fact]
    public void WouldCreateCycle_allows_an_acyclic_dependency()
    {
        CoursePrerequisite[] existing =
        [
            Prerequisite(courseId: 2, prerequisiteCourseId: 3)
        ];

        Assert.False(_validator.WouldCreateCycle(existing, courseId: 1, prerequisiteCourseId: 2));
    }

    private static CoursePrerequisite Prerequisite(
        int courseId,
        int prerequisiteCourseId,
        bool isActive = true)
        => new()
        {
            CourseId = courseId,
            PrerequisiteCourseId = prerequisiteCourseId,
            IsActive = isActive
        };
}
