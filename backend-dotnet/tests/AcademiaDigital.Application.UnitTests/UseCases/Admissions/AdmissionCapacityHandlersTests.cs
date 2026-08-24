using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admissions;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admissions;

public sealed class AdmissionCapacityHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reconcile_expires_overdue_reservations_and_promotes_FIFO()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var form = Form(capacity: 1);
        var expired = Application(1, AdmissionApplicationStatus.PreEnrolled, Now.UtcDateTime.AddHours(-2));
        var firstWaiting = Application(2, AdmissionApplicationStatus.Waitlisted, null);
        var laterWaiting = Application(3, AdmissionApplicationStatus.Waitlisted, null);
        repository.GetExpiredReservationsAsync(form.Id, Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns([expired]);
        repository.CountCapacityOccupyingApplicationsAsync(form.Id, Arg.Any<CancellationToken>())
            .Returns(0);
        repository.GetWaitlistedApplicationsAsync(form.Id, 1, null, Arg.Any<CancellationToken>())
            .Returns([firstWaiting]);
        repository.UpdateApplicationStatusAsync(
                Arg.Any<AdmissionApplication>(),
                Arg.Any<AdmissionApplicationStatusHistory>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionApplication>());
        var coordinator = new AdmissionCapacityCoordinator(repository);

        var result = await coordinator.ReconcileAsync(
            form, Now.UtcDateTime, 99, null, TestContext.Current.CancellationToken);

        Assert.Equal(new AdmissionCapacityReconciliationResult(1, 1), result);
        Assert.Equal(AdmissionApplicationStatus.Expired, expired.Status);
        Assert.Equal(AdmissionApplicationStatus.PreEnrolled, firstWaiting.Status);
        Assert.Equal(Now.UtcDateTime.AddHours(48), firstWaiting.ReservationExpiresAt);
        Assert.Equal(AdmissionApplicationStatus.Waitlisted, laterWaiting.Status);
        Assert.Equal(99, firstWaiting.StatusHistory.Single().ChangedByUserId);
        await repository.Received(2).UpdateApplicationStatusAsync(
            Arg.Any<AdmissionApplication>(),
            Arg.Any<AdmissionApplicationStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCapacity_rejects_a_limit_below_occupied_capacity()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var form = Form(capacity: 3);
        repository.LockFormForCapacityAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        repository.FindFormByIdAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        repository.CountCapacityOccupyingApplicationsAsync(form.Id, Arg.Any<CancellationToken>()).Returns(2);
        var unitOfWork = ImmediateUnitOfWork<AdmissionFormDto>();
        var handler = new SetAdmissionFormCapacityCommandHandler(
            repository,
            new AdmissionCapacityPolicy(),
            new AdmissionTargetPolicy(),
            new AdmissionCapacityCoordinator(repository),
            unitOfWork,
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new SetAdmissionFormCapacityCommand(form.Id, 1, 99),
            TestContext.Current.CancellationToken));

        await repository.DidNotReceive().UpdateFormAsync(
            Arg.Any<AdmissionForm>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetCapacity_rejects_unlimited_capacity_for_a_commission_target()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var form = Form(capacity: 3);
        form.CommissionId = 30;
        repository.LockFormForCapacityAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        repository.FindFormByIdAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        var handler = new SetAdmissionFormCapacityCommandHandler(
            repository,
            new AdmissionCapacityPolicy(),
            new AdmissionTargetPolicy(),
            new AdmissionCapacityCoordinator(repository),
            ImmediateUnitOfWork<AdmissionFormDto>(),
            new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new SetAdmissionFormCapacityCommand(form.Id, null, 99),
            TestContext.Current.CancellationToken));

        await repository.DidNotReceive().UpdateFormAsync(
            Arg.Any<AdmissionForm>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessExpirations_reconciles_only_forms_with_expired_reservations()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var form = Form(capacity: 1);
        repository.GetFormIdsWithExpiredReservationsAsync(Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns([form.Id]);
        repository.LockFormForCapacityAsync(form.Id, Arg.Any<CancellationToken>()).Returns(form);
        repository.GetExpiredReservationsAsync(form.Id, Now.UtcDateTime, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplication>());
        repository.GetWaitlistedApplicationsAsync(form.Id, 1, null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplication>());
        var unitOfWork = ImmediateUnitOfWork<ProcessAdmissionExpirationsDto>();
        var handler = new ProcessAdmissionExpirationsCommandHandler(
            repository,
            new AdmissionCapacityCoordinator(repository),
            unitOfWork,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new ProcessAdmissionExpirationsCommand(null, 99),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.FormsProcessed);
        Assert.Equal(0, result.Expired);
        Assert.Equal(0, result.Promoted);
    }

    private static IUnitOfWork ImmediateUnitOfWork<T>()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<T>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<T>>>()(
                call.ArgAt<CancellationToken>(1)));
        return unitOfWork;
    }

    private static AdmissionForm Form(int? capacity)
        => new()
        {
            Id = 10,
            CareerId = 20,
            Career = new Career { Id = 20, Name = "Backend" },
            Slug = "backend-2027",
            Title = "Ingreso Backend",
            TermsText = "Terms",
            ReservationHours = 48,
            Capacity = capacity,
            Fields = []
        };

    private static AdmissionApplication Application(
        long id,
        AdmissionApplicationStatus status,
        DateTime? expiresAt)
        => new()
        {
            Id = id,
            PublicId = Guid.NewGuid(),
            AdmissionFormId = 10,
            AdmissionForm = Form(1),
            ApplicantEmail = $"applicant{id}@example.com",
            ApplicantDni = $"1234567{id}",
            Status = status,
            CreatedAt = Now.UtcDateTime.AddMinutes(id),
            UpdatedAt = Now.UtcDateTime.AddMinutes(id),
            ReservationExpiresAt = expiresAt
        };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
