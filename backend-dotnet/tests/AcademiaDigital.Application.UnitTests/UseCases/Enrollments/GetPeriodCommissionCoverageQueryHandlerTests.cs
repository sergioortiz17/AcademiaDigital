using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Enrollments;

/// <summary>
/// Parte 11: el diagnóstico de cobertura de comisiones de un período reporta qué combinaciones
/// (YearNumber-con-materias × turno-con-cupo&gt;0) NO tienen comisión activa que matchee. Es
/// read-only y no bloquea nada.
/// </summary>
public sealed class GetPeriodCommissionCoverageQueryHandlerTests
{
    private const int PeriodId = 700;
    private const int CareerId = 7;
    private const int StudyPlanId = 70;
    private const int AcademicYear = 2026;

    [Fact]
    public async Task Reports_no_gaps_when_every_year_and_shift_with_quota_has_a_commission()
    {
        var ctx = CreateContext(
            years: [1],
            quotas: (10, 10, 10),
            // toda combinación (año, turno) tiene comisión
            commissionsByYearShift: (year, shift) => [Commission(1, year, shift)]);

        var result = await ctx.Handler.Handle(new GetPeriodCommissionCoverageQuery(PeriodId), TestContext.Current.CancellationToken);

        Assert.Empty(result.Gaps);
        Assert.Equal(PeriodId, result.PeriodId);
        Assert.Equal(CareerId, result.CareerId);
        Assert.Equal(AcademicYear, result.AcademicYear);
    }

    [Fact]
    public async Task Reports_a_gap_for_a_shift_with_quota_that_has_no_commission()
    {
        // Solo hay comisión para Mañana; Tarde y Noche (con cupo) faltan.
        var ctx = CreateContext(
            years: [1],
            quotas: (10, 10, 10),
            commissionsByYearShift: (year, shift) =>
                shift == "Mañana" ? [Commission(1, year, shift)] : []);

        var result = await ctx.Handler.Handle(new GetPeriodCommissionCoverageQuery(PeriodId), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Gaps.Count);
        Assert.Contains(result.Gaps, g => g.YearNumber == 1 && g.Shift == "Tarde");
        Assert.Contains(result.Gaps, g => g.YearNumber == 1 && g.Shift == "Noche");
        Assert.DoesNotContain(result.Gaps, g => g.Shift == "Mañana");
    }

    [Fact]
    public async Task Does_not_require_a_commission_for_a_shift_with_zero_quota()
    {
        // Solo Mañana tiene cupo; no hay comisiones en ningún turno.
        var ctx = CreateContext(
            years: [1],
            quotas: (10, 0, 0),
            commissionsByYearShift: (_, _) => []);

        var result = await ctx.Handler.Handle(new GetPeriodCommissionCoverageQuery(PeriodId), TestContext.Current.CancellationToken);

        // Solo se reporta el turno con cupo (Mañana); Tarde/Noche con cupo 0 no se exigen.
        var gap = Assert.Single(result.Gaps);
        Assert.Equal(1, gap.YearNumber);
        Assert.Equal("Mañana", gap.Shift);
    }

    [Fact]
    public async Task Checks_every_plan_year_that_has_courses()
    {
        // Materias de 1° y 3°; un solo turno con cupo (Mañana); sin comisiones.
        var ctx = CreateContext(
            years: [1, 3],
            quotas: (5, 0, 0),
            commissionsByYearShift: (_, _) => []);

        var result = await ctx.Handler.Handle(new GetPeriodCommissionCoverageQuery(PeriodId), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Gaps.Count);
        Assert.Contains(result.Gaps, g => g.YearNumber == 1 && g.Shift == "Mañana");
        Assert.Contains(result.Gaps, g => g.YearNumber == 3 && g.Shift == "Mañana");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────

    private static HandlerContext CreateContext(
        int[] years,
        (int Morning, int Afternoon, int Evening) quotas,
        Func<int, string, IReadOnlyList<Commission>> commissionsByYearShift)
    {
        var periodRepository = Substitute.For<IEnrollmentPeriodRepository>();
        var studyPlanCourseRepository = Substitute.For<IStudyPlanCourseRepository>();
        var commissionRepository = Substitute.For<ICommissionRepository>();

        periodRepository.FindByIdAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(new EnrollmentPeriod
            {
                Id = PeriodId, CareerId = CareerId, StudyPlanId = StudyPlanId, AcademicYear = AcademicYear,
                Semester = 1, QuotasMorning = quotas.Morning, QuotasAfternoon = quotas.Afternoon,
                QuotasEvening = quotas.Evening, IsActive = true
            }));

        // Study plan courses: una materia por año, para producir los YearNumber distintos.
        var spcs = years.Select((y, i) => new StudyPlanCourse
        {
            Id = 100 + i, CourseId = 1 + i, StudyPlanId = StudyPlanId, YearNumber = y
        }).ToList();
        studyPlanCourseRepository.GetByStudyPlanIdAsync(StudyPlanId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StudyPlanCourse>>(spcs));

        commissionRepository.FindMatchingActiveAsync(
                CareerId, AcademicYear, Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(commissionsByYearShift(call.ArgAt<int>(2), call.ArgAt<string>(3))));

        var handler = new GetPeriodCommissionCoverageQueryHandler(
            periodRepository, studyPlanCourseRepository, commissionRepository);
        return new HandlerContext(handler);
    }

    private static Commission Commission(int id, int yearNumber, string shift) => new()
    {
        Id = id, CareerId = CareerId, Code = $"COM-{id}", Name = $"Comisión {id}",
        AcademicYear = AcademicYear, YearNumber = yearNumber, Shift = shift, IsActive = true
    };

    private sealed record HandlerContext(GetPeriodCommissionCoverageQueryHandler Handler);
}
