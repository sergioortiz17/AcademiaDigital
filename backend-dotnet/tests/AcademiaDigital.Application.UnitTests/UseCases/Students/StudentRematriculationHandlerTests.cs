using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Students;

public sealed class StudentRematriculationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2027, 2, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_creates_history_and_replaces_the_current_assignment()
    {
        var context = TestContextData();
        var currentAssignment = new StudentAcademicAssignment
        {
            Id = 50,
            StudentCareerId = context.StudentCareer.Id,
            AcademicYear = 2026,
            IsCurrent = true
        };
        var currentPlan = new StudentStudyPlan
        {
            Id = 60,
            StudentCareerId = context.StudentCareer.Id,
            StudyPlanId = context.StudyPlan.Id,
            IsCurrent = true
        };
        context.Rematriculations.GetCurrentAssignmentsAsync(
            context.StudentCareer.Id, Arg.Any<CancellationToken>()).Returns([currentAssignment]);
        context.Rematriculations.FindCurrentStudyPlanAsync(
            context.StudentCareer.Id, Arg.Any<CancellationToken>()).Returns(currentPlan);

        var result = await context.Handler.Handle(Command(), TestContext.Current.CancellationToken);

        Assert.Equal(2027, result.AcademicYear);
        Assert.Equal("Evening", result.Shift);
        Assert.False(currentAssignment.IsCurrent);
        Assert.Equal(Now.UtcDateTime, currentAssignment.EndedAt);
        Assert.True(currentPlan.IsCurrent);
        await context.Rematriculations.Received(1).CreateAsync(
            Arg.Is<StudentRematriculation>(item => item.AcademicYear == 2027 && item.Notes == "Next cycle"),
            Arg.Is<StudentAcademicAssignment>(item =>
                item.AcademicYear == 2027
                && item.IsCurrent
                && item.AssignedByUserId == 99),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_closes_and_replaces_a_different_current_study_plan()
    {
        var context = TestContextData();
        var currentPlan = new StudentStudyPlan
        {
            Id = 60,
            StudentCareerId = context.StudentCareer.Id,
            StudyPlanId = 999,
            IsCurrent = true
        };
        context.Rematriculations.FindCurrentStudyPlanAsync(
            context.StudentCareer.Id, Arg.Any<CancellationToken>()).Returns(currentPlan);

        await context.Handler.Handle(Command(), TestContext.Current.CancellationToken);

        Assert.False(currentPlan.IsCurrent);
        Assert.Equal(Now.UtcDateTime, currentPlan.EndedAt);
        await context.Rematriculations.Received(1).CreateAsync(
            Arg.Any<StudentRematriculation>(),
            Arg.Any<StudentAcademicAssignment>(),
            Arg.Is<StudentStudyPlan>(item => item.StudyPlanId == context.StudyPlan.Id && item.IsCurrent),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_duplicate_cycle_before_mutating_history()
    {
        var context = TestContextData();
        context.Rematriculations.FindByCycleAsync(
            context.StudentCareer.Id, 2027, Arg.Any<CancellationToken>())
            .Returns(new StudentRematriculation());

        await Assert.ThrowsAsync<StudentRematriculationAlreadyExistsException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        await context.Rematriculations.DidNotReceive().CreateAsync(
            Arg.Any<StudentRematriculation>(),
            Arg.Any<StudentAcademicAssignment>(),
            Arg.Any<StudentStudyPlan?>(),
            Arg.Any<CancellationToken>());
    }

    private static CreateStudentRematriculationCommand Command()
        => new(1, 10, 20, 30, 2027, 2, " Next cycle ", 99);

    private static HandlerContext TestContextData()
    {
        var students = Substitute.For<IStudentRepository>();
        var plans = Substitute.For<IStudyPlanRepository>();
        var commissions = Substitute.For<ICommissionRepository>();
        var rematriculations = Substitute.For<IRematriculationRepository>();
        var student = new Student
        {
            Id = 1,
            Status = StudentStatus.Regular,
            User = new User { Id = 100, IsActive = true }
        };
        var studentCareer = new StudentCareer { Id = 11, StudentId = 1, CareerId = 10, IsActive = true };
        var plan = new StudyPlan
        {
            Id = 20,
            CareerId = 10,
            Name = "Plan 2027",
            IsActive = true,
            Status = StudyPlanStatus.Active
        };
        var commission = new Commission
        {
            Id = 30,
            CareerId = 10,
            Name = "Second year evening",
            Shift = "Evening",
            AcademicYear = 2027,
            YearNumber = 2,
            IsActive = true
        };
        students.FindByIdAsync(1, Arg.Any<CancellationToken>()).Returns(student);
        plans.GetByIdAsync(20, Arg.Any<CancellationToken>()).Returns(plan);
        commissions.FindByIdAsync(30, Arg.Any<CancellationToken>()).Returns(commission);
        rematriculations.LockActiveStudentCareerAsync(1, 10, Arg.Any<CancellationToken>())
            .Returns(studentCareer);
        rematriculations.GetLatestAcademicYearAsync(11, Arg.Any<CancellationToken>()).Returns(2026);
        rematriculations.GetCurrentAssignmentsAsync(11, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<StudentAcademicAssignment>());
        rematriculations.CreateAsync(
                Arg.Any<StudentRematriculation>(),
                Arg.Any<StudentAcademicAssignment>(),
                Arg.Any<StudentStudyPlan?>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var item = call.Arg<StudentRematriculation>();
                item.Id = 70;
                return item;
            });
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<StudentRematriculationDto>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<StudentRematriculationDto>>>()(
                call.ArgAt<CancellationToken>(1)));
        var handler = new CreateStudentRematriculationCommandHandler(
            students,
            plans,
            commissions,
            rematriculations,
            new StudentRematriculationPolicy(),
            unitOfWork,
            new FixedTimeProvider(Now));
        return new HandlerContext(handler, rematriculations, studentCareer, plan);
    }

    private sealed record HandlerContext(
        CreateStudentRematriculationCommandHandler Handler,
        IRematriculationRepository Rematriculations,
        StudentCareer StudentCareer,
        StudyPlan StudyPlan);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
