using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Platform;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Platform;

public sealed class PlatformRequestTests
{
    private readonly ITenantRegistry _registry = Substitute.For<ITenantRegistry>();
    private readonly IPlatformRequestStore _store = Substitute.For<IPlatformRequestStore>();
    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private SubmitPlatformRequestHandler Submit() => new(new TenantPolicy(), _registry, _store, _email, TimeProvider.System);

    private static SubmitPlatformRequest Valid(string? slug = "usb", string? website = null) => new(
        "Universidad Simón Bolívar", slug, "Ana Pérez", " Ana@USB.edu ", "+58 212 000", "Caracas", 1200,
        "Queremos probar la plataforma.", website);

    [Fact]
    public async Task Valid_request_is_saved_and_the_operator_is_notified()
    {
        var result = await Submit().Handle(Valid(), "operador@example.com", Ct);

        Assert.NotNull(result);
        Assert.Equal("usb", result!.RequestedSlug);
        Assert.Equal("ana@usb.edu", result.ContactEmail);
        Assert.Equal(PlatformRequestStatus.Pending, result.Status);
        await _store.Received(1).AddAsync(Arg.Any<PlatformRequest>(), Arg.Any<CancellationToken>());
        await _email.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m => m.To == "operador@example.com" && m.Subject.Contains("Simón Bolívar")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Without_notify_address_the_request_is_still_saved()
    {
        await Submit().Handle(Valid(), notifyEmail: null, Ct);
        await _store.Received(1).AddAsync(Arg.Any<PlatformRequest>(), Arg.Any<CancellationToken>());
        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Fact]
    public async Task Honeypot_filled_by_a_bot_is_discarded_silently()
    {
        var result = await Submit().Handle(Valid(website: "http://spam"), "operador@example.com", Ct);

        Assert.Null(result);
        await _store.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _email.DidNotReceiveWithAnyArgs().SendAsync(default!, default);
    }

    [Theory]
    [InlineData("admin")]           // subdominio reservado del sitio
    [InlineData("academiadigital")]
    [InlineData("u sb")]            // formato inválido
    public async Task Reserved_or_invalid_subdomains_are_rejected(string slug)
        => await Assert.ThrowsAsync<ArgumentException>(() => Submit().Handle(Valid(slug), null, Ct));

    [Fact]
    public async Task Subdomain_already_in_use_is_rejected()
    {
        _registry.SlugExistsAsync("usb", Arg.Any<CancellationToken>()).Returns(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Submit().Handle(Valid(), null, Ct));
    }

    [Fact]
    public async Task Invalid_email_is_rejected()
        => await Assert.ThrowsAsync<ArgumentException>(() =>
            Submit().Handle(Valid() with { ContactEmail = "no-es-un-email" }, null, Ct));

    [Fact]
    public async Task Approving_requires_an_existing_institution()
    {
        var pending = new PlatformRequest { Id = Guid.NewGuid(), Status = PlatformRequestStatus.Pending };
        _store.FindAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);
        var handler = new ReviewPlatformRequestHandler(_store, _registry, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(pending.Id, new ReviewPlatformRequest(PlatformRequestStatus.Approved, "usb"), Ct));

        _registry.SlugExistsAsync("usb", Arg.Any<CancellationToken>()).Returns(true);
        var approved = await handler.Handle(pending.Id, new ReviewPlatformRequest(PlatformRequestStatus.Approved, "USB"), Ct);

        Assert.Equal(PlatformRequestStatus.Approved, approved.Status);
        Assert.Equal("usb", approved.TenantSlug);
        Assert.NotNull(approved.ReviewedAt);
    }

    [Fact]
    public async Task A_reviewed_request_cannot_be_reviewed_again()
    {
        var done = new PlatformRequest { Id = Guid.NewGuid(), Status = PlatformRequestStatus.Rejected };
        _store.FindAsync(done.Id, Arg.Any<CancellationToken>()).Returns(done);
        var handler = new ReviewPlatformRequestHandler(_store, _registry, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(done.Id, new ReviewPlatformRequest(PlatformRequestStatus.Approved, "usb"), Ct));
    }
}
