using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Enrollments;

public sealed class UpdatePeriodQuotasCommandHandlerTests
{
    [Fact]
    public async Task Handle_rejects_a_quota_below_current_occupancy()
    {
        var context = CreateContext((Morning: 0, Afternoon: 2, Evening: 0));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(
                new UpdatePeriodQuotasCommand(10, 5, 1, 5),
                TestContext.Current.CancellationToken));

        Assert.Equal("Los cupos de inscripción no pueden ser menores que la ocupación actual.", exception.Message);
        await context.Repository.DidNotReceive()
            .UpdateAsync(Arg.Any<EnrollmentPeriod>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_updates_quotas_inside_a_serializable_transaction()
    {
        var context = CreateContext((Morning: 1, Afternoon: 2, Evening: 0));

        var result = await context.Handler.Handle(
            new UpdatePeriodQuotasCommand(10, 2, 3, 1),
            TestContext.Current.CancellationToken);

        await context.UnitOfWork.Received(1).ExecuteInSerializableTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<bool>>>(),
            Arg.Any<CancellationToken>());
        Assert.Equal(2, context.Period.QuotasMorning);
        Assert.Equal(3, context.Period.QuotasAfternoon);
        Assert.Equal(1, context.Period.QuotasEvening);
        Assert.Equal(2, result.QuotasMorning);
        Assert.Equal(2, result.EnrolledAfternoon);
    }

    private static HandlerContext CreateContext((int Morning, int Afternoon, int Evening) counts)
    {
        var repository = Substitute.For<IEnrollmentPeriodRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var period = new EnrollmentPeriod
        {
            Id = 10,
            CareerId = 1,
            Career = DomainTestFactory.Career(id: 1, name: "Career"),
            StudyPlanId = 2,
            StudyPlan = DomainTestFactory.StudyPlan(id: 2, name: "Plan"),
            AcademicYear = 2026,
            Semester = 1,
            QuotasMorning = 1,
            QuotasAfternoon = 2,
            QuotasEvening = 1
        };

        repository.LockForEnrollmentAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(period));
        repository.FindByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(period));
        repository.GetEnrolledShiftCountsAsync(10, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(counts));
        repository.UpdateAsync(period, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(period));
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(call.ArgAt<CancellationToken>(1)));

        var handler = new UpdatePeriodQuotasCommandHandler(
            repository,
            new EnrollmentCapacityPolicy(),
            unitOfWork,
            TimeProvider.System);
        return new HandlerContext(handler, repository, unitOfWork, period);
    }

    private sealed record HandlerContext(
        UpdatePeriodQuotasCommandHandler Handler,
        IEnrollmentPeriodRepository Repository,
        IUnitOfWork UnitOfWork,
        EnrollmentPeriod Period);
}
