using AcademiaDigital.Application.UseCases.User;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Profile;

public sealed class ProfileContactUseCaseTests
{
    private readonly IProfileContactRepository repository = Substitute.For<IProfileContactRepository>();

    private static ProfileContactInfo Info(string? phone = "351 555-1234", string? insurance = " OSDE ")
        => new(" Calle 1 ", "Córdoba", "Córdoba", "5000", "Ana", "Madre", phone, insurance);

    [Fact]
    public async Task Saves_trimmed_values_and_blank_as_null()
    {
        repository.SaveAsync(7, Arg.Any<ProfileContactInfo>(), Arg.Any<CancellationToken>()).Returns(true);

        var saved = await new ProfileContactUseCase(repository).SaveAsync(7, Info() with { PostalCode = "  " },
            TestContext.Current.CancellationToken);

        Assert.Equal("Calle 1", saved.AddressLine);
        Assert.Equal("OSDE", saved.HealthInsurance);
        Assert.Null(saved.PostalCode);
    }

    [Fact]
    public async Task Rejects_an_emergency_phone_with_letters_or_too_many_digits()
    {
        var useCase = new ProfileContactUseCase(repository);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SaveAsync(7, Info(phone: "35A5551234"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.SaveAsync(7, Info(phone: "1111111111111111"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Accounts_without_a_student_or_teacher_profile_are_rejected()
    {
        repository.SaveAsync(7, Arg.Any<ProfileContactInfo>(), Arg.Any<CancellationToken>()).Returns(false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new ProfileContactUseCase(repository).SaveAsync(7, Info(), TestContext.Current.CancellationToken));
    }
}
