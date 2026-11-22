using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using NSubstitute;
using Xunit;
using UserEntity = AcademiaDigital.Domain.Entities.User;

namespace AcademiaDigital.Application.UnitTests.UseCases.Authentication;

public sealed class GoogleLoginTests
{
    private readonly IUserRepository users = Substitute.For<IUserRepository>();
    private readonly ISessionRepository sessions = Substitute.For<ISessionRepository>();
    private readonly ITokenService tokens = Substitute.For<ITokenService>();
    private readonly IGoogleIdentityVerifier google = Substitute.For<IGoogleIdentityVerifier>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public GoogleLoginTests()
    {
        google.ClientId.Returns("client.apps.googleusercontent.com");
        tokens.GenerateToken(Arg.Any<UserEntity>()).Returns("jwt");
    }

    private LoginUseCase UseCase() => new(users, sessions, tokens, google);

    private static UserEntity Account(RegistrationStatus status = RegistrationStatus.Approved, bool active = true)
        => new() { Id = 7, Email = "ana@gmail.com", Username = "Ana", Role = UserRole.Profesor, IsActive = active, RegistrationStatus = status };

    [Fact]
    public async Task An_existing_account_signs_in_with_its_google_email()
    {
        google.VerifyAsync("token", Arg.Any<CancellationToken>()).Returns(new GoogleIdentity("ana@gmail.com", true, "Ana"));
        users.FindByEmailAsync("ana@gmail.com", Arg.Any<CancellationToken>()).Returns(Account());

        var result = await UseCase().ExecuteWithGoogleAsync("token", Ct);

        Assert.Equal("jwt", result.Token);
        Assert.Equal(UserRole.Profesor, result.User.Role);
    }

    [Fact]
    public async Task An_unknown_email_is_not_registered_automatically()
    {
        google.VerifyAsync("token", Arg.Any<CancellationToken>()).Returns(new GoogleIdentity("otra@gmail.com", true, null));

        await Assert.ThrowsAsync<GoogleAccountNotRegisteredException>(() => UseCase().ExecuteWithGoogleAsync("token", Ct));
        await users.DidNotReceive().CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invalid_tokens_or_unverified_emails_are_rejected()
    {
        google.VerifyAsync("bad", Arg.Any<CancellationToken>()).Returns((GoogleIdentity?)null);
        google.VerifyAsync("unverified", Arg.Any<CancellationToken>()).Returns(new GoogleIdentity("ana@gmail.com", false, null));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => UseCase().ExecuteWithGoogleAsync("bad", Ct));
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => UseCase().ExecuteWithGoogleAsync("unverified", Ct));
    }

    [Fact]
    public async Task Pending_or_inactive_accounts_cannot_sign_in_with_google()
    {
        google.VerifyAsync("token", Arg.Any<CancellationToken>()).Returns(new GoogleIdentity("ana@gmail.com", true, null));
        users.FindByEmailAsync("ana@gmail.com", Arg.Any<CancellationToken>()).Returns(Account(RegistrationStatus.Pending));
        await Assert.ThrowsAsync<PendingApprovalException>(() => UseCase().ExecuteWithGoogleAsync("token", Ct));

        users.FindByEmailAsync("ana@gmail.com", Arg.Any<CancellationToken>()).Returns(Account(active: false));
        await Assert.ThrowsAsync<InactiveUserException>(() => UseCase().ExecuteWithGoogleAsync("token", Ct));
    }

    [Fact]
    public async Task Google_login_is_off_without_a_client_id()
    {
        google.ClientId.Returns(string.Empty);
        await Assert.ThrowsAsync<InvalidOperationException>(() => UseCase().ExecuteWithGoogleAsync("token", Ct));
    }
}
