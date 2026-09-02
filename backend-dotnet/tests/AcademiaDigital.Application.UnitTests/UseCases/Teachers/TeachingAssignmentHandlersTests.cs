using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Teachers;

public sealed class TeachingAssignmentHandlersTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePosition_validates_relations_and_creates_a_vacancy()
    {
        var positions = Substitute.For<ITeachingPositionRepository>();
        var courses = Substitute.For<ICourseRepository>();
        var commissions = Substitute.For<ICommissionRepository>();
        courses.FindByIdAsync(2, Arg.Any<CancellationToken>()).Returns(Course());
        commissions.FindByIdAsync(3, Arg.Any<CancellationToken>()).Returns(Commission());
        positions.CreateAsync(Arg.Any<TeachingPosition>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var position = call.Arg<TeachingPosition>();
                position.Id = 5;
                return position;
            });
        var handler = new CreateTeachingPositionCommandHandler(
            positions, courses, commissions, new TeachingAssignmentPolicy(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateTeachingPositionCommand(
            2, 3, 2027, 1, PositionType.Titular, 40), TestContext.Current.CancellationToken);

        Assert.Equal(5, result.Id);
        Assert.True(result.IsVacant);
        Assert.True(result.IsActive);
        Assert.Equal(Now.UtcDateTime, result.CreatedAt);
    }

    [Fact]
    public async Task AssignTeacher_uses_a_serializable_transaction()
    {
        var teachers = Substitute.For<ITeacherRepository>();
        var positions = Substitute.For<ITeachingPositionRepository>();
        var assignments = Substitute.For<ITeacherAssignmentRepository>();
        var unitOfWork = SerializableUnitOfWork();
        teachers.FindByIdAsync(4, Arg.Any<CancellationToken>()).Returns(Teacher());
        positions.FindByIdAsync(5, Arg.Any<CancellationToken>()).Returns(Position());
        assignments.AssignAsync(Arg.Any<TeacherAssignment>(), Arg.Any<CancellationToken>())
            .Returns(call => Assignment(call.Arg<TeacherAssignment>()));
        var handler = new AssignTeacherCommandHandler(
            teachers, positions, assignments, new TeachingAssignmentPolicy(), unitOfWork,
            new FixedTimeProvider(Now));

        var result = await handler.Handle(new AssignTeacherCommand(
            4, 5, new DateOnly(2027, 3, 1), " Inicio de ciclo ", 99),
            TestContext.Current.CancellationToken);

        Assert.True(result.IsCurrent);
        Assert.Equal("Inicio de ciclo", result.AssignmentReason);
        await unitOfWork.Received(1).ExecuteInSerializableTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<TeacherAssignment>>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetMyAssignments_resolves_the_teacher_from_the_authenticated_user()
    {
        var teachers = Substitute.For<ITeacherRepository>();
        var assignments = Substitute.For<ITeacherAssignmentRepository>();
        teachers.FindByUserIdAsync(88, Arg.Any<CancellationToken>()).Returns(Teacher());
        assignments.GetByTeacherAsync(4, false, Arg.Any<CancellationToken>())
            .Returns(new[] { Assignment() });
        var handler = new GetMyTeacherAssignmentsQueryHandler(teachers, assignments);

        var result = await handler.Handle(
            new GetMyTeacherAssignmentsQuery(88, false), TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(4, result[0].TeacherId);
    }

    [Fact]
    public async Task EndAssignment_validates_and_closes_inside_a_serializable_transaction()
    {
        var assignments = Substitute.For<ITeacherAssignmentRepository>();
        var unitOfWork = SerializableUnitOfWork();
        var existing = Assignment();
        assignments.FindAsync(4, 7, Arg.Any<CancellationToken>()).Returns(existing);
        assignments.EndAsync(4, 7, Arg.Any<DateOnly>(), Arg.Any<DateTime>(), 99, Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                existing.IsCurrent = false;
                existing.EndedOn = call.ArgAt<DateOnly>(2);
                existing.EndReason = call.ArgAt<string>(5).Trim();
                return existing;
            });
        var handler = new EndTeacherAssignmentCommandHandler(
            assignments, new TeachingAssignmentPolicy(), unitOfWork, new FixedTimeProvider(Now));

        var result = await handler.Handle(new EndTeacherAssignmentCommand(
            4, 7, new DateOnly(2027, 7, 1), " Reasignación ", 99),
            TestContext.Current.CancellationToken);

        Assert.False(result.IsCurrent);
        Assert.Equal("Reasignación", result.EndReason);
    }

    private static IUnitOfWork SerializableUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<TeacherAssignment>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<TeacherAssignment>>>()(
                call.ArgAt<CancellationToken>(1)));
        return unitOfWork;
    }

    private static Course Course() => new()
    {
        Id = 2, CareerId = 10, Code = "ARQ", Name = "Arquitectura", IsActive = true
    };
    private static Commission Commission() => new()
    {
        Id = 3, CareerId = 10, Code = "C1", Name = "Comisión 1", AcademicYear = 2027, IsActive = true
    };
    private static User ProfessorUser() => new()
    {
        Id = 88, Username = "Ada", LastName = "Lovelace", IsActive = true, Role = UserRole.Profesor
    };
    private static Teacher Teacher() => new() { Id = 4, IsActive = true, UserId = 88, User = ProfessorUser() };
    private static TeachingPosition Position() => new()
    {
        Id = 5,
        CourseId = 2,
        Course = Course(),
        CommissionId = 3,
        Commission = Commission(),
        AcademicYear = 2027,
        Semester = 1,
        PositionType = PositionType.Titular,
        MaxStudents = 40,
        IsVacant = true,
        IsActive = true,
        CreatedAt = Now.UtcDateTime,
        UpdatedAt = Now.UtcDateTime
    };
    private static TeacherAssignment Assignment(TeacherAssignment? source = null)
    {
        var assignment = source ?? new TeacherAssignment
        {
            TeacherId = 4,
            TeachingPositionId = 5,
            StartedOn = new DateOnly(2027, 3, 1),
            IsCurrent = true,
            CreatedAt = Now.UtcDateTime
        };
        assignment.Id = 7;
        assignment.Teacher = Teacher();
        assignment.TeachingPosition = Position();
        return assignment;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
