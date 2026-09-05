using AcademiaDigital.Application.UseCases.Admissions;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admissions;

public sealed class AdmissionAdminHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateForm_normalizes_and_persists_an_active_form()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var careers = Substitute.For<ICareerRepository>();
        careers.FindByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(DomainTestFactory.Career(id: 20, name: "Backend", isActive: true));
        repository.CreateFormAsync(Arg.Any<AdmissionForm>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionForm>());
        var commissions = Substitute.For<ICommissionRepository>();
        var handler = new CreateAdmissionFormCommandHandler(
            repository, careers, commissions, new AdmissionFormPolicy(), new AdmissionCapacityPolicy(),
            new AdmissionTargetPolicy(), new FixedTimeProvider(Now));

        var result = await handler.Handle(ValidCreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("backend-2027", result.Slug);
        Assert.True(result.IsActive);
        Assert.Equal("Backend", result.CareerName);
        await repository.Received(1).CreateFormAsync(
            Arg.Is<AdmissionForm>(form =>
                form.CareerId == 20
                && form.CreatedAt == Now.UtcDateTime
                && form.Fields.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateForm_rejects_a_duplicate_slug_before_persisting()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var careers = Substitute.For<ICareerRepository>();
        careers.FindByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(DomainTestFactory.Career(id: 20, name: "Backend", isActive: true));
        repository.FormSlugExistsAsync("backend-2027", Arg.Any<CancellationToken>()).Returns(true);
        var commissions = Substitute.For<ICommissionRepository>();
        var handler = new CreateAdmissionFormCommandHandler(
            repository, careers, commissions, new AdmissionFormPolicy(), new AdmissionCapacityPolicy(),
            new AdmissionTargetPolicy(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<AdmissionFormSlugAlreadyExistsException>(() =>
            handler.Handle(ValidCreateCommand(), TestContext.Current.CancellationToken));

        await repository.DidNotReceive().CreateFormAsync(
            Arg.Any<AdmissionForm>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateForm_persists_a_compatible_commission_target()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var careers = Substitute.For<ICareerRepository>();
        var commissions = Substitute.For<ICommissionRepository>();
        var career = DomainTestFactory.Career(id: 20, name: "Backend", isActive: true);
        var commission = new Commission
        {
            Id = 30,
            CareerId = 20,
            Code = "B1",
            Name = "Backend Noche",
            AcademicYear = 2027,
            YearNumber = 1,
            Shift = "Evening",
            IsActive = true
        };
        careers.FindByIdAsync(20, Arg.Any<CancellationToken>()).Returns(career);
        commissions.FindByIdAsync(30, Arg.Any<CancellationToken>()).Returns(commission);
        repository.CreateFormAsync(Arg.Any<AdmissionForm>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionForm>());
        var handler = new CreateAdmissionFormCommandHandler(
            repository, careers, commissions, new AdmissionFormPolicy(), new AdmissionCapacityPolicy(),
            new AdmissionTargetPolicy(), new FixedTimeProvider(Now));

        var result = await handler.Handle(
            ValidCreateCommand() with { CommissionId = 30, Capacity = 25 },
            TestContext.Current.CancellationToken);

        Assert.Equal(30, result.CommissionId);
        Assert.Equal("B1", result.CommissionCode);
        Assert.Equal("Evening", result.Shift);
        Assert.Equal(25, result.Capacity);
        await repository.Received(1).CreateFormAsync(
            Arg.Is<AdmissionForm>(form => form.CommissionId == 30 && form.Capacity == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateForm_rejects_a_commission_already_assigned_to_a_form()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var careers = Substitute.For<ICareerRepository>();
        var commissions = Substitute.For<ICommissionRepository>();
        careers.FindByIdAsync(20, Arg.Any<CancellationToken>())
            .Returns(DomainTestFactory.Career(id: 20, name: "Backend", isActive: true));
        commissions.FindByIdAsync(30, Arg.Any<CancellationToken>())
            .Returns(new Commission { Id = 30, CareerId = 20, IsActive = true });
        repository.CommissionTargetExistsAsync(30, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateAdmissionFormCommandHandler(
            repository, careers, commissions, new AdmissionFormPolicy(), new AdmissionCapacityPolicy(),
            new AdmissionTargetPolicy(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<AdmissionCommissionAlreadyAssignedException>(() => handler.Handle(
            ValidCreateCommand() with { CommissionId = 30, Capacity = 25 },
            TestContext.Current.CancellationToken));

        await repository.DidNotReceive().CreateFormAsync(
            Arg.Any<AdmissionForm>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_appends_an_audited_transition()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var application = ValidApplication(AdmissionApplicationStatus.PreEnrolled);
        repository.FindApplicationByPublicIdAsync(application.PublicId, true, Arg.Any<CancellationToken>())
            .Returns(application);
        repository.UpdateApplicationStatusAsync(
                Arg.Any<AdmissionApplication>(),
                Arg.Any<AdmissionApplicationStatusHistory>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionApplication>());
        var handler = CreateChangeHandler(repository, application);

        var result = await handler.Handle(
            new ChangeAdmissionApplicationStatusCommand(
                application.PublicId,
                AdmissionApplicationStatus.Enrolled,
                "Documentación verificada",
                99),
            TestContext.Current.CancellationToken);

        Assert.Equal("Enrolled", result.Application.Status);
        Assert.Single(result.History);
        Assert.Equal(99, result.History[0].ChangedByUserId);
        Assert.Equal("Documentación verificada", result.History[0].Reason);
        await repository.Received(1).UpdateApplicationStatusAsync(
            application,
            Arg.Is<AdmissionApplicationStatusHistory>(history =>
                history.FromStatus == AdmissionApplicationStatus.PreEnrolled
                && history.ToStatus == AdmissionApplicationStatus.Enrolled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_refreshes_the_reservation_when_reactivating_a_waitlisted_application()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var application = ValidApplication(AdmissionApplicationStatus.Waitlisted);
        application.AdmissionForm.ReservationHours = 48;
        repository.FindApplicationByPublicIdAsync(application.PublicId, true, Arg.Any<CancellationToken>())
            .Returns(application);
        repository.UpdateApplicationStatusAsync(
                Arg.Any<AdmissionApplication>(),
                Arg.Any<AdmissionApplicationStatusHistory>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionApplication>());
        var handler = CreateChangeHandler(repository, application);

        await handler.Handle(
            new ChangeAdmissionApplicationStatusCommand(
                application.PublicId,
                AdmissionApplicationStatus.PreEnrolled,
                null,
                99),
            TestContext.Current.CancellationToken);

        Assert.Equal(Now.UtcDateTime.AddHours(48), application.ReservationExpiresAt);
    }

    [Fact]
    public async Task ChangeStatus_blocks_confirmation_until_required_documents_are_approved()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var application = ValidApplication(AdmissionApplicationStatus.Enrolled);
        repository.FindApplicationByPublicIdAsync(application.PublicId, true, Arg.Any<CancellationToken>())
            .Returns(application);
        var handler = CreateChangeHandler(repository, application);
        repository.GetApplicableRequiredDocumentRequirementsAsync(
                application.AdmissionForm.CareerId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns([new DocumentRequirement { Id = 8, Code = "DNI", IsRequired = true, IsActive = true }]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new ChangeAdmissionApplicationStatusCommand(
                application.PublicId,
                AdmissionApplicationStatus.Confirmed,
                null,
                99),
            TestContext.Current.CancellationToken));

        Assert.Contains("DNI", exception.Message);
        await repository.DidNotReceive().UpdateApplicationStatusAsync(
            Arg.Any<AdmissionApplication>(),
            Arg.Any<AdmissionApplicationStatusHistory>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatus_creates_agreement_and_outbox_in_the_confirmation_transaction()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var application = ValidApplication(AdmissionApplicationStatus.Enrolled);
        repository.FindApplicationByPublicIdAsync(application.PublicId, true, Arg.Any<CancellationToken>())
            .Returns(application);
        repository.UpdateApplicationStatusAsync(
                Arg.Any<AdmissionApplication>(),
                Arg.Any<AdmissionApplicationStatusHistory>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionApplication>());
        repository.CreateAgreementWithOutboxAsync(
                Arg.Any<AdmissionAgreement>(), Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionAgreement>());
        var handler = CreateChangeHandler(repository, application);

        await handler.Handle(new ChangeAdmissionApplicationStatusCommand(
            application.PublicId,
            AdmissionApplicationStatus.Confirmed,
            null,
            99), TestContext.Current.CancellationToken);

        await repository.Received(1).CreateAgreementWithOutboxAsync(
            Arg.Is<AdmissionAgreement>(agreement =>
                agreement.AdmissionApplicationId == application.Id
                && agreement.AgreementNumber.StartsWith("ADM-")),
            Arg.Is<OutboxMessage>(message =>
                message.Type == "AdmissionAgreementConfirmed"
                && message.DeduplicationKey.Contains(application.PublicId.ToString("N"))),
            Arg.Any<CancellationToken>());
    }

    private static CreateAdmissionFormCommand ValidCreateCommand()
        => new(
            20,
            null,
            " Backend-2027 ",
            "Ingreso Backend 2027",
            null,
            "Terms",
            72,
            null,
            [
                new AdmissionFormFieldInput("email", "Email", AdmissionFieldType.Email, true, 1),
                new AdmissionFormFieldInput("dni", "DNI", AdmissionFieldType.Text, true, 2)
            ]);

    private static AdmissionApplication ValidApplication(AdmissionApplicationStatus status)
    {
        var form = new AdmissionForm
        {
            Id = 10,
            CareerId = 20,
            Career = DomainTestFactory.Career(id: 20, name: "Backend"),
            Slug = "backend-2027",
            Title = "Ingreso Backend",
            ReservationHours = 72
        };
        return new AdmissionApplication
        {
            Id = 30,
            PublicId = Guid.NewGuid(),
            AdmissionFormId = form.Id,
            AdmissionForm = form,
            ApplicantEmail = "ada@example.com",
            ApplicantDni = "12345678",
            SubmittedFieldsJson = "{\"email\":\"ada@example.com\",\"dni\":\"12345678\"}",
            Status = status,
            ReservationExpiresAt = Now.UtcDateTime.AddHours(24)
        };
    }

    private static ChangeAdmissionApplicationStatusCommandHandler CreateChangeHandler(
        IAdmissionRepository repository,
        AdmissionApplication application)
    {
        repository.LockFormForCapacityAsync(application.AdmissionFormId, Arg.Any<CancellationToken>())
            .Returns(application.AdmissionForm);
        repository.GetExpiredReservationsAsync(
                application.AdmissionFormId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplication>());
        repository.GetWaitlistedApplicationsAsync(
                application.AdmissionFormId, Arg.Any<int>(), application.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplication>());
        repository.GetApplicableRequiredDocumentRequirementsAsync(
                application.AdmissionForm.CareerId, Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<DocumentRequirement>());
        repository.GetApplicationDocumentsAsync(
                application.Id, false, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplicationDocument>());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<AdmissionApplicationDetailDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<AdmissionApplicationDetailDto>>>()(
                call.ArgAt<CancellationToken>(1)));
        return new ChangeAdmissionApplicationStatusCommandHandler(
            repository,
            new AdmissionStatusTransitionPolicy(),
            new AdmissionDocumentPolicy(),
            new AdmissionCapacityPolicy(),
            new AdmissionCapacityCoordinator(repository),
            unitOfWork,
            new FixedTimeProvider(Now));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
