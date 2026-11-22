using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admin;

public sealed class ImportApplicantsUseCaseTests
{
    private readonly ICareerRepository careers = Substitute.For<ICareerRepository>();
    private readonly IUserRepository users = Substitute.For<IUserRepository>();

    private ImportApplicantsUseCase UseCase() => new(new RegisterUseCase(users,
        Substitute.For<IStudentRepository>(), Substitute.For<IStudentCareerRepository>(), careers,
        Substitute.For<IStudyPlanRepository>(), Substitute.For<IStudentAcademicRepository>(),
        Substitute.For<ITeacherRepository>(), Substitute.For<IUnitOfWork>()), careers);

    [Fact]
    public async Task Invalid_rows_are_reported_by_row_number_without_creating_accounts()
    {
        var result = await UseCase().ExecuteAsync(
        [
            new(2, "123", "Perez", "Ana", "ana@x.com", "DS2023"),
            new(3, "30111222", "", "Ana", "ana@x.com", "DS2023"),
            new(4, "30111222", "Perez", "Ana", "no-es-correo", "DS2023"),
            new(5, "30111222", "Perez", "Ana", "ana@x.com", "")
        ], TestContext.Current.CancellationToken);

        Assert.Equal(0, result.Created);
        Assert.Equal([2, 3, 4, 5], result.Failed.Select(f => f.RowNumber));
        Assert.Contains("DNI", result.Failed[0].Reason);
    }

    [Fact]
    public async Task Unknown_career_codes_are_reported()
    {
        var result = await UseCase().ExecuteAsync([new(2, "30.111.222", "Perez", "Ana", "ana@x.com", "XX")],
            TestContext.Current.CancellationToken);

        Assert.Contains("XX", Assert.Single(result.Failed).Reason);
    }

    [Fact]
    public async Task An_empty_file_is_rejected()
        => await Assert.ThrowsAsync<ArgumentException>(() => UseCase().ExecuteAsync([], TestContext.Current.CancellationToken));
}
