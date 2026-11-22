using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Teachers;

public sealed class CourseSectionListTests
{
    [Fact]
    public async Task List_InformsEnrolledStudentsPerSection_ForFreeSeats()
    {
        var course = new Course { Id = 7, Code = "MAT1", Name = "Matemática" };
        var withStudents = new CourseSection { Id = 1, CourseId = 7, Course = course, MaxStudents = 30, AcademicYear = 2026, Semester = 1 };
        var empty = new CourseSection { Id = 2, CourseId = 7, Course = course, MaxStudents = 25, AcademicYear = 2026, Semester = 1 };
        var repository = Substitute.For<ICourseSectionRepository>();
        repository.GetAllAsync(2026, null, null, false, Arg.Any<CancellationToken>())
            .Returns(new List<CourseSection> { withStudents, empty });
        repository.CountEnrolledAsync(Arg.Any<IReadOnlyCollection<CourseSection>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [1] = 4 });

        var result = await new GetCourseSectionsQueryHandler(repository)
            .Handle(new GetCourseSectionsQuery(2026, null, null, false));

        Assert.Equal(4, result.Single(s => s.Id == 1).EnrolledCount);
        Assert.Equal(0, result.Single(s => s.Id == 2).EnrolledCount);
    }
}
