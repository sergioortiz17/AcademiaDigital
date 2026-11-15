using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Admin;

/// <summary>
/// Sección "Aspirantes": el registro público deja la cuenta pendiente, el dominio docente crea
/// un Profesor sin carrera, un pendiente no puede iniciar sesión y el alta en masa activa
/// usuarios y perfiles docentes.
/// </summary>
public sealed class ApplicantsTests
{
    private const string TeacherDomain = "itscordoba.edu.ar";
    private const int CareerId = 7;

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ITeacherRepository _teachers = Substitute.For<ITeacherRepository>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ICareerRepository _careers = Substitute.For<ICareerRepository>();
    private readonly IStudyPlanRepository _plans = Substitute.For<IStudyPlanRepository>();
    private readonly IStudentCareerRepository _studentCareers = Substitute.For<IStudentCareerRepository>();
    private readonly IStudentAcademicRepository _academic = Substitute.For<IStudentAcademicRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly List<User> _created = [];

    public ApplicantsTests()
    {
        _users.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.FindByDniAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);
        _users.CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<UserRole>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var user = new User { Id = 100 + _created.Count, Email = call.ArgAt<string>(0), Role = call.ArgAt<UserRole>(5) };
                _created.Add(user);
                return user;
            });
        _users.UpdateAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<User>());
        _teachers.CreateAsync(Arg.Any<Teacher>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<Teacher>());
        _students.CreateAsync(Arg.Any<Student>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<Student>());
        _studentCareers.CreateAsync(Arg.Any<StudentCareer>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<StudentCareer>());
        _academic.AssignStudyPlanAsync(Arg.Any<StudentStudyPlan>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<StudentStudyPlan>());
        _careers.FindByIdAsync(CareerId, Arg.Any<CancellationToken>())
            .Returns(DomainTestFactory.Career(id: CareerId, name: "Software", isActive: true));
        _plans.GetActiveByCareerIdAsync(CareerId, Arg.Any<CancellationToken>())
            .Returns(DomainTestFactory.StudyPlan(id: 70, careerId: CareerId, name: "Plan", status: StudyPlanStatus.Active));
        _uow.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<RegisterResult>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<RegisterResult>>>()(call.ArgAt<CancellationToken>(1)));
    }

    private RegisterUseCase Register() => new(_users, _students, _studentCareers, _careers, _plans, _academic, _teachers, _uow);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Public_registration_of_a_student_is_pending()
    {
        var result = await Register().ExecutePublicAsync(
            "alumna@gmail.com", "Ana", "Paz", "Alumna123!", "40111222", CareerId, TeacherDomain, Ct);

        Assert.True(result.PendingApproval);
        Assert.Equal(UserRole.Alumno, result.Role);
        Assert.Equal(RegistrationStatus.Pending, Assert.Single(_created).RegistrationStatus);
        await _students.Received(1).CreateAsync(Arg.Any<Student>(), Arg.Any<CancellationToken>());
        await _teachers.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Theory]
    [InlineData("maria.quintar@itscordoba.edu.ar")]
    [InlineData("Maria.Quintar@ITSCORDOBA.EDU.AR")]
    public async Task Email_of_the_teacher_domain_registers_a_pending_teacher_without_career(string email)
    {
        var result = await Register().ExecutePublicAsync(
            email, "María", "Quintar", "Docente123!", "30111222", careerId: null, TeacherDomain, Ct);

        Assert.True(result.PendingApproval);
        Assert.Equal(UserRole.Profesor, result.Role);
        var user = Assert.Single(_created);
        Assert.Equal(UserRole.Profesor, user.Role);
        Assert.Equal(RegistrationStatus.Pending, user.RegistrationStatus);
        await _teachers.Received(1).CreateAsync(Arg.Is<Teacher>(t => t.UserId == user.Id && !t.IsActive), Arg.Any<CancellationToken>());
        await _students.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Theory]
    [InlineData("alguien@sub.itscordoba.edu.ar")]   // subdominio: no es el dominio docente
    [InlineData("itscordoba.edu.ar@gmail.com")]
    public async Task Other_emails_need_a_career(string email)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => Register().ExecutePublicAsync(
            email, "X", "Y", "Clave123!", "30999888", careerId: null, TeacherDomain, Ct));
        Assert.Contains("carrera", ex.Message);
        Assert.Empty(_created);
    }

    [Fact]
    public async Task Without_teacher_domain_configured_nobody_becomes_teacher()
    {
        var result = await Register().ExecutePublicAsync(
            "maria.quintar@itscordoba.edu.ar", "María", "Quintar", "Docente123!", "30111222", CareerId,
            teacherEmailDomain: null, Ct);

        Assert.Equal(UserRole.Alumno, result.Role);
    }

    [Fact]
    public async Task Direct_registration_used_by_devtools_stays_approved()
    {
        var result = await Register().ExecuteAsync("dev@test.local", "Dev", "Tools", "Clave123!", "30000001", CareerId, Ct);

        Assert.False(result.PendingApproval);
        Assert.Equal(RegistrationStatus.Approved, Assert.Single(_created).RegistrationStatus);
    }

    [Fact]
    public async Task A_pending_user_cannot_log_in_even_with_the_right_password()
    {
        var pending = new User { Id = 5, Email = "a@b.c", IsActive = true, RegistrationStatus = RegistrationStatus.Pending };
        _users.FindByEmailAsync("a@b.c", Arg.Any<CancellationToken>()).Returns(pending);
        _users.AuthenticateAsync("a@b.c", "Clave123!", Arg.Any<CancellationToken>()).Returns(pending);
        var sessions = Substitute.For<ISessionRepository>();
        var login = new LoginUseCase(_users, sessions, Substitute.For<ITokenService>());

        await Assert.ThrowsAsync<PendingApprovalException>(() => login.ExecuteAsync("a@b.c", "Clave123!", Ct));
        await sessions.DidNotReceiveWithAnyArgs().CreateAsync(default, default!, default);
    }

    [Fact]
    public async Task Bulk_approval_activates_teacher_profiles_and_reports_skipped_ids()
    {
        var teacherUser = new User { Id = 1, Role = UserRole.Profesor };
        var studentUser = new User { Id = 2, Role = UserRole.Alumno };
        _users.ApproveRegistrationsAsync(Arg.Any<IReadOnlyCollection<long>>(), 99, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns([teacherUser, studentUser]);
        var profile = new Teacher { Id = 10, UserId = 1, IsActive = false };
        _teachers.FindByUserIdAsync(1, Arg.Any<CancellationToken>()).Returns(profile);
        _teachers.UpdateAsync(Arg.Any<Teacher>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<Teacher>());

        var result = await new ApproveApplicantsUseCase(_users, _teachers)
            .ExecuteAsync(approvedByUserId: 99, userIds: [1, 2, 2, 3], Ct);

        Assert.Equal([1L, 2L], result.ApprovedIds);
        Assert.Equal(1, result.Skipped); // el 3 no estaba pendiente
        Assert.True(profile.IsActive);
        await _users.Received(1).ApproveRegistrationsAsync(
            Arg.Is<IReadOnlyCollection<long>>(ids => ids.SequenceEqual(new long[] { 1, 2, 3 })), 99, Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Bulk_approval_requires_a_selection()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            new ApproveApplicantsUseCase(_users, _teachers).ExecuteAsync(99, [], Ct));
    }
}
