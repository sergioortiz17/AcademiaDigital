using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class ExamSchedulerTests
{
    // Lunes 1 a viernes 5 de febrero de 2027.
    private static readonly DateOnly[] Week = Enumerable.Range(1, 5).Select(d => new DateOnly(2027, 2, d)).ToArray();

    [Fact]
    public void Courses_of_the_same_year_never_share_a_day()
    {
        var result = ExamScheduler.Propose(
        [
            new(1, 1, 1, []), new(2, 1, 2, []), new(3, 1, 3, []), new(10, 2, 1, [])
        ], Week);

        var firstYear = result.Where(r => r.CourseId is 1 or 2 or 3).Select(r => r.Date).ToArray();
        Assert.Equal(3, firstYear.Distinct().Count());
        Assert.Equal(Week[0], result.Single(r => r.CourseId == 10).Date);
    }

    [Fact]
    public void A_course_goes_after_its_prerequisites()
    {
        // Estadística (2.º año) tiene de correlativa a Elementos de Matemática (1.º año).
        var result = ExamScheduler.Propose([new(20, 2, 1, [1]), new(1, 1, 1, [])], Week);

        Assert.True(result.Single(r => r.CourseId == 20).Date > result.Single(r => r.CourseId == 1).Date);
    }

    [Fact]
    public void Courses_that_do_not_fit_report_why()
    {
        var result = ExamScheduler.Propose([new(1, 1, 1, []), new(2, 1, 2, []), new(3, 1, 3, [1])], Week.Take(2).ToArray());

        var third = result.Single(r => r.CourseId == 3);
        Assert.Null(third.Date);
        Assert.NotNull(third.Reason);
    }

    [Fact]
    public void Business_days_skip_weekends_and_holidays()
    {
        var days = ExamScheduler.BusinessDays(new DateOnly(2027, 2, 5), new DateOnly(2027, 2, 9), [new DateOnly(2027, 2, 8)]);
        Assert.Equal([new DateOnly(2027, 2, 5), new DateOnly(2027, 2, 9)], days);
    }
}
