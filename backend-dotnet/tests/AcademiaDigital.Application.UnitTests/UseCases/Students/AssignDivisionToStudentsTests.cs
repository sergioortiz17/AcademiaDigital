using AcademiaDigital.Application.Dtos;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Students;

public sealed class AssignDivisionToStudentsTests
{
    private const int CareerId = 3;
    private readonly IDivisionRepository _divisions = Substitute.For<IDivisionRepository>();
    private readonly IStudentAcademicRepository _academic = Substitute.For<IStudentAcademicRepository>();
    private readonly IStudentManagementService _students = Substitute.For<IStudentManagementService>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private AssignDivisionToStudentsHandler Handler() => new(_divisions, _academic, _students);

    public AssignDivisionToStudentsTests()
    {
        _divisions.FindByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Division
        {
            Id = 10, CareerId = CareerId, AcademicYear = 2026, YearNumber = 1, IsActive = true, Code = "C"
        });
        foreach (var id in new long[] { 1, 2 })
            _academic.GetCurrentStudyPlanAsync(id, CareerId, Arg.Any<CancellationToken>())
                .Returns(new StudentStudyPlan { StudentId = id, StudyPlanId = 70 });
    }

    [Fact]
    public async Task Assigns_each_student_with_its_current_plan_and_the_division_cycle()
    {
        var result = await Handler().Handle(CareerId, 10, [1, 2, 2], "Inicio de cursada", actorId: 99, Ct);

        Assert.Equal([1L, 2L], result.Assigned);
        Assert.Empty(result.Failed);
        await _students.Received(1).AssignAcademicAsync(1, Arg.Is<CreateAcademicAssignmentRequest>(r =>
            r.CareerId == CareerId && r.StudyPlanId == 70 && r.DivisionId == 10 && r.AcademicYear == 2026
            && r.YearNumber == 1 && r.Reason == "Inicio de cursada"), 99, Arg.Any<CancellationToken>());
        await _students.Received(2).AssignAcademicAsync(Arg.Any<long>(), Arg.Any<CreateAcademicAssignmentRequest>(), 99, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_student_that_fails_does_not_stop_the_rest()
    {
        // 3 no tiene plan vigente; 2 falla en la validación de la asignación.
        _students.AssignAcademicAsync(2, Arg.Any<CreateAcademicAssignmentRequest>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("El alumno no está inscripto activamente en la carrera seleccionada."));

        var result = await Handler().Handle(CareerId, 10, [1, 2, 3], null, 99, Ct);

        Assert.Equal([1L], result.Assigned);
        Assert.Equal([2L, 3L], result.Failed.Select(f => f.StudentId).Order());
        Assert.Contains("plan de estudios vigente", result.Failed.Single(f => f.StudentId == 3).Reason);
    }

    [Fact]
    public async Task Division_of_another_career_is_not_found()
        => await Assert.ThrowsAsync<KeyNotFoundException>(() => Handler().Handle(CareerId + 1, 10, [1], null, 99, Ct));

    [Fact]
    public async Task Requires_a_selection()
        => await Assert.ThrowsAsync<ArgumentException>(() => Handler().Handle(CareerId, 10, [], null, 99, Ct));
}
