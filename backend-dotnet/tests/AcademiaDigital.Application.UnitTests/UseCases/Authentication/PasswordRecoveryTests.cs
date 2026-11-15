using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Authentication;

/// <summary>
/// Recuperación de contraseña por correo: el token nunca vuelve en la respuesta, viaja por
/// email, es de una sola institución y sirve una sola vez.
/// </summary>
public sealed class PasswordRecoveryTests
{
    private const string ResetPage = "https://itsc.dryvn.org/auth/reset-password";

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITokenService _tokens = Substitute.For<ITokenService>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ISessionRepository _sessions = Substitute.For<ISessionRepository>();

    private static User AnUser(bool active = true) => new()
    {
        Id = 7, Email = "ana@itsc.edu.ar", Username = "Ana", Dni = "30111222",
        Password = "$2a$11$hash-actual", IsActive = active
    };

    private ForgotPasswordUseCase Forgot(string tenant = "itsc")
        => new(_users, _tokens, _email, TestTenant.Context(tenant));

    private ResetPasswordUseCase Reset(string tenant = "itsc")
        => new(_users, _tokens, _hasher, _sessions, TestTenant.Context(tenant));

    [Fact]
    public async Task Forgot_sends_a_link_with_the_token_bound_to_the_tenant_and_password()
    {
        var user = AnUser();
        _users.FindByEmailAsync("ana@itsc.edu.ar", Arg.Any<CancellationToken>()).Returns(user);
        _tokens.GeneratePasswordResetToken(7, "itsc", PasswordResetStamp.For(user.Password)).Returns("tok+en/1");

        await Forgot().ExecuteAsync(" ana@itsc.edu.ar ", "30111222", ResetPage);

        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "ana@itsc.edu.ar"
                && m.TextBody.Contains($"{ResetPage}?token=tok%2Ben%2F1")
                && m.HtmlBody.Contains("tok%2Ben%2F1")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Forgot_does_not_send_anything_for_unknown_email()
    {
        await Forgot().ExecuteAsync("nadie@itsc.edu.ar", null, ResetPage);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Forgot_does_not_send_anything_when_the_dni_does_not_match()
    {
        _users.FindByEmailAsync("ana@itsc.edu.ar", Arg.Any<CancellationToken>()).Returns(AnUser());

        await Forgot().ExecuteAsync("ana@itsc.edu.ar", "99999999", ResetPage);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Forgot_does_not_send_anything_for_inactive_users()
    {
        _users.FindByEmailAsync("ana@itsc.edu.ar", Arg.Any<CancellationToken>()).Returns(AnUser(active: false));

        await Forgot().ExecuteAsync("ana@itsc.edu.ar", null, ResetPage);

        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Reset_changes_the_password_and_closes_open_sessions()
    {
        var user = AnUser();
        _tokens.ReadPasswordResetToken("t").Returns(new PasswordResetClaims(7, "itsc", PasswordResetStamp.For(user.Password)));
        _users.FindByIdAsync(7, Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Hash("NuevaClave123!").Returns("$2a$11$hash-nuevo");

        await Reset().ExecuteAsync("t", "NuevaClave123!");

        await _users.Received(1).UpdatePasswordAsync(7, "$2a$11$hash-nuevo", Arg.Any<CancellationToken>());
        await _sessions.Received(1).DeleteByUserAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reset_rejects_a_token_issued_by_another_institution()
    {
        var user = AnUser();
        _tokens.ReadPasswordResetToken("t").Returns(new PasswordResetClaims(7, "otra", PasswordResetStamp.For(user.Password)));
        _users.FindByIdAsync(7, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<InvalidResetTokenException>(() => Reset("itsc").ExecuteAsync("t", "NuevaClave123!"));
        await _users.DidNotReceiveWithAnyArgs().UpdatePasswordAsync(default, default!, default);
    }

    [Fact]
    public async Task Reset_rejects_a_token_already_used()
    {
        // La contraseña cambió después de emitir el token: la huella ya no coincide.
        var user = AnUser();
        _tokens.ReadPasswordResetToken("t").Returns(new PasswordResetClaims(7, "itsc", PasswordResetStamp.For("$2a$11$hash-anterior")));
        _users.FindByIdAsync(7, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<InvalidResetTokenException>(() => Reset().ExecuteAsync("t", "NuevaClave123!"));
        await _users.DidNotReceiveWithAnyArgs().UpdatePasswordAsync(default, default!, default);
    }

    [Fact]
    public async Task Reset_rejects_an_invalid_or_expired_token()
    {
        _tokens.ReadPasswordResetToken("t").Returns((PasswordResetClaims?)null);

        await Assert.ThrowsAsync<InvalidResetTokenException>(() => Reset().ExecuteAsync("t", "NuevaClave123!"));
    }
}
