using System.Text.Json;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admissions;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admissions;

public sealed class AdmissionHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetForm_returns_public_configuration_with_ordered_fields()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        repository.FindActiveFormBySlugAsync("backend-2027", Arg.Any<CancellationToken>())
            .Returns(ValidForm());
        var handler = new GetAdmissionFormQueryHandler(repository, new AdmissionFormPolicy());

        var result = await handler.Handle(
            new GetAdmissionFormQuery(" BACKEND-2027 "),
            TestContext.Current.CancellationToken);

        Assert.Equal("backend-2027", result.Slug);
        Assert.Equal("Backend", result.CareerName);
        Assert.Equal(["email", "dni", "firstName"], result.Fields.Select(field => field.Key));
    }

    [Fact]
    public async Task GetForm_returns_not_found_for_an_unknown_or_inactive_slug()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var handler = new GetAdmissionFormQueryHandler(repository, new AdmissionFormPolicy());

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(new GetAdmissionFormQuery("missing-form"), TestContext.Current.CancellationToken));

        Assert.Equal("Admission form not found.", exception.Message);
    }

    [Fact]
    public async Task CreateApplication_rejects_an_existing_email_or_dni()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        repository.FindActiveFormBySlugAsync("backend-2027", Arg.Any<CancellationToken>())
            .Returns(ValidForm());
        repository.ApplicationExistsAsync(10, "ada@example.com", "12345678", Arg.Any<CancellationToken>())
            .Returns(true);
        var handler = CreateHandler(repository);

        await Assert.ThrowsAsync<AdmissionApplicationAlreadyExistsException>(() =>
            handler.Handle(ValidCommand(), TestContext.Current.CancellationToken));

        await repository.DidNotReceive().CreateApplicationAsync(
            Arg.Any<AdmissionApplication>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateApplication_rejects_a_failed_challenge_before_reading_the_form()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var challengeVerifier = Substitute.For<IAdmissionChallengeVerifier>();
        challengeVerifier.VerifyAsync("invalid-token", "203.0.113.10", Arg.Any<CancellationToken>())
            .Returns(false);
        var handler = CreateHandler(repository, challengeVerifier: challengeVerifier);

        var exception = await Assert.ThrowsAsync<AdmissionChallengeRejectedException>(() =>
            handler.Handle(
                ValidCommand() with
                {
                    ChallengeToken = "invalid-token",
                    RemoteIpAddress = "203.0.113.10"
                },
                TestContext.Current.CancellationToken));

        Assert.Equal("Admission challenge verification failed.", exception.Message);
        await repository.DidNotReceive().FindActiveFormBySlugAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateApplication_persists_a_pre_enrollment_with_a_deterministic_expiration()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        repository.FindActiveFormBySlugAsync("backend-2027", Arg.Any<CancellationToken>())
            .Returns(ValidForm());
        repository.CreateApplicationAsync(
                Arg.Any<AdmissionApplication>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionApplication>());
        var handler = CreateHandler(repository);

        var result = await handler.Handle(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.PublicId);
        Assert.Equal("PreEnrolled", result.Status);
        Assert.Equal(Now.UtcDateTime.AddHours(72), result.ReservationExpiresAt);
        await repository.Received(1).CreateApplicationAsync(
            Arg.Is<AdmissionApplication>(application =>
                application.ApplicantEmail == "ada@example.com"
                && application.ApplicantDni == "12345678"
                && application.Status == AdmissionApplicationStatus.PreEnrolled
                && application.TermsAcceptedAt == Now.UtcDateTime
                && application.StatusHistory.Count == 1
                && application.StatusHistory.Single().ToStatus == AdmissionApplicationStatus.PreEnrolled
                && JsonHasField(application.SubmittedFieldsJson, "firstName", "Ada")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateApplication_places_the_applicant_on_the_waitlist_when_capacity_is_full()
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var form = ValidForm();
        form.Capacity = 1;
        repository.FindActiveFormBySlugAsync("backend-2027", Arg.Any<CancellationToken>()).Returns(form);
        repository.CreateApplicationAsync(
                Arg.Any<AdmissionApplication>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<AdmissionApplication>());
        var handler = CreateHandler(repository, capacity: 1, occupied: 1);

        var result = await handler.Handle(ValidCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("Waitlisted", result.Status);
        Assert.Null(result.ReservationExpiresAt);
        await repository.Received(1).CreateApplicationAsync(
            Arg.Is<AdmissionApplication>(application =>
                application.Status == AdmissionApplicationStatus.Waitlisted
                && application.ReservationExpiresAt == null
                && application.StatusHistory.Single().ToStatus == AdmissionApplicationStatus.Waitlisted),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("--")]
    [InlineData("invalid_slug")]
    [InlineData("-invalid")]
    public async Task CreateApplication_rejects_invalid_slugs_before_querying_the_repository(string slug)
    {
        var repository = Substitute.For<IAdmissionRepository>();
        var handler = CreateHandler(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(ValidCommand() with { FormSlug = slug }, TestContext.Current.CancellationToken));

        await repository.DidNotReceive().FindActiveFormBySlugAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private static CreateAdmissionApplicationCommandHandler CreateHandler(
        IAdmissionRepository repository,
        int? capacity = null,
        int occupied = 0,
        IAdmissionChallengeVerifier? challengeVerifier = null)
    {
        var form = ValidForm();
        form.Capacity = capacity;
        repository.LockFormForCapacityAsync(10, Arg.Any<CancellationToken>()).Returns(form);
        repository.CountCapacityOccupyingApplicationsAsync(10, Arg.Any<CancellationToken>()).Returns(occupied);
        repository.GetExpiredReservationsAsync(10, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplication>());
        repository.GetWaitlistedApplicationsAsync(10, Arg.Any<int>(), null, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<AdmissionApplication>());
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<AdmissionApplicationDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<AdmissionApplicationDto>>>()(
                call.ArgAt<CancellationToken>(1)));
        if (challengeVerifier is null)
        {
            challengeVerifier = Substitute.For<IAdmissionChallengeVerifier>();
            challengeVerifier.VerifyAsync(
                    Arg.Any<string?>(),
                    Arg.Any<string?>(),
                    Arg.Any<CancellationToken>())
                .Returns(true);
        }
        return new CreateAdmissionApplicationCommandHandler(
            repository,
            new AdmissionApplicationPolicy(),
            new AdmissionFormPolicy(),
            new AdmissionCapacityPolicy(),
            new AdmissionCapacityCoordinator(repository),
            challengeVerifier,
            unitOfWork,
            new FixedTimeProvider(Now));
    }

    private static CreateAdmissionApplicationCommand ValidCommand()
        => new(
            "backend-2027",
            AcceptedTerms: true,
            new Dictionary<string, string?>
            {
                ["email"] = "Ada@Example.com",
                ["dni"] = "12345678",
                ["firstName"] = "Ada"
            });

    private static AdmissionForm ValidForm()
        => new()
        {
            Id = 10,
            CareerId = 20,
            Career = new Career { Id = 20, Name = "Backend" },
            Slug = "backend-2027",
            Title = "Ingreso Backend 2027",
            TermsText = "Terms",
            ReservationHours = 72,
            IsActive = true,
            Fields =
            [
                Field("firstName", order: 3),
                Field("dni", order: 2),
                Field("email", order: 1, AdmissionFieldType.Email)
            ]
        };

    private static AdmissionFormField Field(
        string key,
        int order,
        AdmissionFieldType type = AdmissionFieldType.Text)
        => new() { Key = key, Label = key, Type = type, IsRequired = true, SortOrder = order };

    private static bool JsonHasField(string json, string key, string value)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(key).GetString() == value;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
