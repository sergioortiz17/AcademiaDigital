using AcademiaDigital.API.Controllers;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace AcademiaDigital.ArchitectureTests;

public sealed class ArchitectureDependencyTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(User).Assembly,
            typeof(IUnitOfWork).Assembly,
            typeof(AppDbContext).Assembly,
            typeof(ApiControllerBase).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer = LayerOf<User>("Domain");
    private static readonly IObjectProvider<IType> ApplicationLayer = LayerOf<IUnitOfWork>("Application");
    private static readonly IObjectProvider<IType> InfrastructureLayer = LayerOf<AppDbContext>("Infrastructure");
    private static readonly IObjectProvider<IType> ApiLayer = LayerOf<ApiControllerBase>("API");

    private static readonly IObjectProvider<Class> ControllersWithoutLegacyExceptions = Classes()
        .That()
        .AreAssignableTo(typeof(ApiControllerBase))
        .And()
        .DoNotHaveFullName(typeof(CalendarController).FullName!)
        .As("API controllers without documented legacy exceptions");

    [Fact]
    public void Domain_must_not_depend_on_Application()
        => MustNotDependOn(DomainLayer, ApplicationLayer, "Domain must remain independent from Application");

    [Fact]
    public void Domain_must_not_depend_on_Infrastructure()
        => MustNotDependOn(DomainLayer, InfrastructureLayer, "Domain must remain independent from Infrastructure");

    [Fact]
    public void Domain_must_not_depend_on_API()
        => MustNotDependOn(DomainLayer, ApiLayer, "Domain must remain independent from API");

    [Fact]
    public void Application_must_not_depend_on_Infrastructure()
        => MustNotDependOn(ApplicationLayer, InfrastructureLayer, "Application may only depend on Domain");

    [Fact]
    public void Application_must_not_depend_on_API()
        => MustNotDependOn(ApplicationLayer, ApiLayer, "Application must remain independent from API");

    [Fact]
    public void Infrastructure_must_not_depend_on_API()
        => MustNotDependOn(InfrastructureLayer, ApiLayer, "Infrastructure must remain independent from API");

    [Fact]
    public void Controllers_must_not_depend_on_Infrastructure_except_documented_legacy_cases()
    {
        Classes()
            .That()
            .Are(ControllersWithoutLegacyExceptions)
            .Should()
            .NotDependOnAny(InfrastructureLayer)
            .Because("controllers must delegate to Application; CalendarController is the temporary documented exception")
            .Check(Architecture);
    }

    [Fact]
    public void AcademiaDigital_layers_must_be_free_of_cycles()
    {
        SliceRuleDefinition.Slices()
            .Matching("AcademiaDigital.(*)")
            .Should()
            .BeFreeOfCycles()
            .Check(Architecture);
    }

    private static IObjectProvider<IType> LayerOf<TMarker>(string name)
        => Types()
            .That()
            .ResideInAssembly(typeof(TMarker).Assembly)
            .As($"{name} layer");

    private static void MustNotDependOn(
        IObjectProvider<IType> source,
        IObjectProvider<IType> forbidden,
        string reason)
    {
        Types()
            .That()
            .Are(source)
            .Should()
            .NotDependOnAny(forbidden)
            .Because(reason)
            .Check(Architecture);
    }
}
