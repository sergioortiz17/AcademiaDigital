using AcademiaDigital.Application.DTOs;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Tenants;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Tenants;

public sealed class CreateTenantCommandHandlerTests
{
    private readonly ITenantRegistry _registry = Substitute.For<ITenantRegistry>();
    private readonly ITenantProvisioner _provisioner = Substitute.For<ITenantProvisioner>();

    private CreateTenantCommandHandler Handler() => new(new TenantPolicy(), _registry, _provisioner);

    private static CreateTenantRequest Request(string slug = "UCV", string[]? domains = null) => new(
        slug, "  Universidad Central  ", domains ?? ["HTTP://ucv.localhost:4200"],
        "admin@ucv.edu", "Ana", "Pérez", "30111222", "Cambiar123!");

    [Fact]
    public async Task Provisions_with_normalized_values_and_derived_database_name()
    {
        _registry.SlugExistsAsync("ucv", Arg.Any<CancellationToken>()).Returns(false);
        _registry.FindTakenHostsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>()).Returns([]);
        _provisioner.ProvisionAsync(Arg.Any<ProvisionTenantRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TenantSummaryDto(Guid.NewGuid(), "ucv", "Universidad Central", true, false, DateTime.UtcNow, ["ucv.localhost"]));

        await Handler().Handle(Request(), TestContext.Current.CancellationToken);

        await _provisioner.Received(1).ProvisionAsync(
            Arg.Is<ProvisionTenantRequest>(r => r.Slug == "ucv" && r.Name == "Universidad Central"
                && r.DatabaseName == "academiadigital_ucv" && r.Hosts.SequenceEqual(new[] { "ucv.localhost" })
                && r.AdminEmail == "admin@ucv.edu"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_an_existing_slug_without_provisioning()
    {
        _registry.SlugExistsAsync("ucv", Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler().Handle(Request(), TestContext.Current.CancellationToken));

        await _provisioner.DidNotReceiveWithAnyArgs().ProvisionAsync(default!, default);
    }

    [Fact]
    public async Task Rejects_hosts_already_assigned_to_another_institution()
    {
        _registry.SlugExistsAsync("ucv", Arg.Any<CancellationToken>()).Returns(false);
        _registry.FindTakenHostsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(["ucv.localhost"]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Handler().Handle(Request(), TestContext.Current.CancellationToken));

        Assert.Contains("ucv.localhost", ex.Message);
        await _provisioner.DidNotReceiveWithAnyArgs().ProvisionAsync(default!, default);
    }

    [Fact]
    public async Task Rejects_invalid_input_before_touching_the_registry()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => Handler().Handle(Request(slug: "bad slug!"), TestContext.Current.CancellationToken));

        await _registry.DidNotReceiveWithAnyArgs().SlugExistsAsync(default!, default);
    }
}
