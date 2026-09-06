using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Teachers;

public sealed class TeacherHandlersTests
{
    private static readonly DateTimeOffset Now = new(2027, 3, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_normalizes_and_persists_a_professor_profile()
    {
        var users = Substitute.For<IUserRepository>();
        var teachers = Substitute.For<ITeacherRepository>();
        var user = Professor();
        var navigationWasEmptyAtPersistence = false;
        users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        teachers.CreateAsync(Arg.Any<Teacher>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var teacher = call.Arg<Teacher>();
            navigationWasEmptyAtPersistence = teacher.User is null;
            teacher.Id = 20;
            return teacher;
        });
        var handler = CreateHandler(users, teachers);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(20, result.Id);
        Assert.Equal("DOC-001", result.EmployeeNumber);
        Assert.Equal("Engineering", result.Department);
        Assert.True(navigationWasEmptyAtPersistence);
        await teachers.Received(1).CreateAsync(
            Arg.Is<Teacher>(teacher => teacher.UserId == user.Id && teacher.City == "Rosario"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_a_user_without_professor_role()
    {
        var users = Substitute.For<IUserRepository>();
        var teachers = Substitute.For<ITeacherRepository>();
        var user = Professor();
        user.Role = UserRole.Alumno;
        users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler(users, teachers).Handle(CreateCommand(), TestContext.Current.CancellationToken));

        await teachers.DidNotReceive().CreateAsync(Arg.Any<Teacher>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_rejects_a_user_already_linked_to_a_teacher()
    {
        var users = Substitute.For<IUserRepository>();
        var teachers = Substitute.For<ITeacherRepository>();
        var user = Professor();
        users.FindByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        teachers.FindByUserIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(new Teacher { Id = 50 });

        await Assert.ThrowsAsync<TeacherAlreadyExistsException>(() =>
            CreateHandler(users, teachers).Handle(CreateCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Update_rejects_an_employee_number_owned_by_another_teacher()
    {
        var teachers = Substitute.For<ITeacherRepository>();
        teachers.FindByIdAsync(20, Arg.Any<CancellationToken>()).Returns(TeacherProfile(20));
        teachers.FindByEmployeeNumberAsync("DOC-001", Arg.Any<CancellationToken>())
            .Returns(TeacherProfile(21));
        var handler = new UpdateTeacherCommandHandler(
            teachers, new TeacherProfilePolicy(), new FixedTimeProvider(Now));
        var command = new UpdateTeacherCommand(
            20, "doc-001", null, null, new DateTime(2020, 1, 1), null,
            null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<TeacherAlreadyExistsException>(() =>
            handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Deactivate_records_actor_time_and_reason_once()
    {
        var teachers = Substitute.For<ITeacherRepository>();
        var teacher = TeacherProfile(20);
        teachers.FindByIdAsync(20, Arg.Any<CancellationToken>()).Returns(teacher);
        var handler = new DeactivateTeacherCommandHandler(
            teachers, new TeacherProfilePolicy(), new FixedTimeProvider(Now));

        await handler.Handle(new DeactivateTeacherCommand(20, 99, " End of appointment "),
            TestContext.Current.CancellationToken);

        Assert.False(teacher.IsActive);
        Assert.Equal(Now.UtcDateTime, teacher.DeactivatedAt);
        Assert.Equal(99, teacher.DeactivatedByUserId);
        Assert.Equal("End of appointment", teacher.DeactivationReason);
        await teachers.Received(1).UpdateAsync(teacher, Arg.Any<CancellationToken>());
    }

    private static CreateTeacherCommandHandler CreateHandler(
        IUserRepository users,
        ITeacherRepository teachers)
        => new(users, teachers, new TeacherProfilePolicy(), new FixedTimeProvider(Now));

    private static CreateTeacherCommand CreateCommand() => new(
        10, " doc-001 ", " Engineering ", " Software ", new DateTime(2020, 1, 1),
        "3410000000", " Main 123 ", " Rosario ", " Santa Fe ", "2000",
        "Contact", "Sibling", "3411111111");

    private static User Professor() => new()
    {
        Id = 10,
        Username = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.edu",
        Dni = "12345678",
        Role = UserRole.Profesor,
        IsActive = true
    };

    private static Teacher TeacherProfile(long id) => new()
    {
        Id = id,
        UserId = 10,
        User = Professor(),
        EmployeeNumber = $"DOC-{id}",
        HireDate = new DateTime(2020, 1, 1),
        IsActive = true
    };

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
