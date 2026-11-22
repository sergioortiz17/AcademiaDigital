using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admin;

public sealed class UpdateUserRoleUseCaseTests
{
    private readonly IUserRepository users = Substitute.For<IUserRepository>();
    private readonly ITeacherRepository teachers = Substitute.For<ITeacherRepository>();
    private readonly IStudentRepository students = Substitute.For<IStudentRepository>();

    private UpdateUserRoleUseCase UseCase() => new(users, teachers, students, new ImmediateUnitOfWork());

    [Fact]
    public async Task Changing_to_professor_creates_the_missing_teacher_profile()
    {
        users.UpdateRoleAsync(7, UserRole.Profesor, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 7, Role = UserRole.Profesor, Email = "a@b.c", Username = "Ana" });

        var result = await UseCase().ExecuteAsync(1, 7, UserRole.Profesor, TestContext.Current.CancellationToken);

        Assert.Equal(UserRole.Profesor, result.Role);
        await teachers.Received(1).CreateAsync(
            Arg.Is<Teacher>(t => t.UserId == 7 && t.IsActive && t.EmployeeNumber == "DOC-00007"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Changing_to_professor_keeps_an_existing_teacher_profile()
    {
        users.UpdateRoleAsync(7, UserRole.Profesor, Arg.Any<CancellationToken>())
            .Returns(new User { Id = 7, Role = UserRole.Profesor, Email = "a@b.c", Username = "Ana" });
        teachers.FindByUserIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Teacher { Id = 3, UserId = 7 });

        await UseCase().ExecuteAsync(1, 7, UserRole.Profesor, TestContext.Current.CancellationToken);

        await teachers.DidNotReceive().CreateAsync(Arg.Any<Teacher>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Changing_to_student_without_student_profile_is_rejected()
    {
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UseCase().ExecuteAsync(1, 7, UserRole.Alumno, TestContext.Current.CancellationToken));

        Assert.Contains("perfil de alumno", error.Message);
        await users.DidNotReceive().UpdateRoleAsync(Arg.Any<long>(), Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
