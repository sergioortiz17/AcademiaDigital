using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Authentication;

/// <summary>
/// Regresión de la Parte 9: el registro público de un alumno (RegisterUseCase) debe dejarlo con un
/// StudentStudyPlan vigente, de modo que su PRIMERA inscripción no falle con "El alumno no tiene un
/// plan de estudios actual...". Antes RegisterUseCase creaba User+Student+StudentCareer pero NO el
/// StudentStudyPlan.
///
/// El test enlaza los dos casos de uso reales (registrar → inscribir) SIN pasos manuales de por
/// medio: ambos comparten un mismo "store" en memoria de StudentStudyPlans. Así, si el registro
/// vuelve a olvidar el plan, la inscripción falla de verdad (los fakes no fabrican el plan).
/// </summary>
public sealed class RegisterThenEnrollTests
{
    private const int CareerId = 7;
    private const int StudyPlanId = 70;
    // El plan Active de la carrera es el mismo que el del período de inscripción (caso real): el
    // alumno se registra, queda con ese plan vigente, y el período abierto es para ese mismo plan.
    private const int ActivePlanId = StudyPlanId;
    private const int PeriodId = 700;

    [Fact]
    public async Task A_newly_registered_student_can_enroll_without_any_manual_study_plan_step()
    {
        var store = new FakeStore();
        var register = BuildRegisterUseCase(store, hasActivePlan: true);
        var enroll = BuildEnrollHandler(store);

        // 1) Registro público real del alumno.
        var result = await register.ExecuteAsync(
            email: "nuevo.alumno@test.local", name: "Nuevo", lastName: "Alumno",
            password: "Alumno123!", dni: "70123456", careerId: CareerId,
            ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        // El registro dejó un StudentStudyPlan vigente (el hueco de la Parte 9).
        var current = Assert.Single(store.StudentStudyPlans);
        Assert.True(current.IsCurrent);
        Assert.Equal(ActivePlanId, current.StudyPlanId);

        // 2) Primera inscripción del mismo alumno, en el mismo flujo, sin tocar nada a mano.
        //    No debe tirar "El alumno no tiene un plan de estudios actual...".
        await enroll.Handle(
            new CreateEnrollmentCommand(store.StudentId, PeriodId, "Tarde", [101], store.UserId),
            TestContext.Current.CancellationToken);

        Assert.Single(store.CreatedEnrollments);
    }

    [Fact]
    public async Task Registration_fails_clearly_when_the_career_has_no_active_study_plan()
    {
        var store = new FakeStore();
        var register = BuildRegisterUseCase(store, hasActivePlan: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            register.ExecuteAsync(
                email: "sin.plan@test.local", name: "Sin", lastName: "Plan",
                password: "Alumno123!", dni: "70999999", careerId: CareerId,
                ct: TestContext.Current.CancellationToken));

        Assert.Contains("no tiene un plan de estudios vigente", exception.Message);
        // No se creó nada: ni alumno con plan colgado ni inscripción posible.
        Assert.Empty(store.StudentStudyPlans);
    }

    // ── Construcción de los casos de uso con fakes que comparten el mismo store ───────────────

    private static RegisterUseCase BuildRegisterUseCase(FakeStore store, bool hasActivePlan)
    {
        var userRepository = Substitute.For<IUserRepository>();
        var studentRepository = Substitute.For<IStudentRepository>();
        var studentCareerRepository = Substitute.For<IStudentCareerRepository>();
        var careerRepository = Substitute.For<ICareerRepository>();
        var studyPlanRepository = Substitute.For<IStudyPlanRepository>();
        var studentAcademicRepository = Substitute.For<IStudentAcademicRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        careerRepository.FindByIdAsync(CareerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Career?>(DomainTestFactory.Career(id: CareerId, name: "Carrera Test", isActive: true)));
        studyPlanRepository.GetActiveByCareerIdAsync(CareerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(hasActivePlan
                ? DomainTestFactory.StudyPlan(id: ActivePlanId, careerId: CareerId, name: "Plan Activo", status: StudyPlanStatus.Active)
                : null));
        userRepository.FindByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));
        userRepository.FindByDniAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(null));
        userRepository.CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<UserRole>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new User { Id = store.UserId, Email = "nuevo.alumno@test.local" }));
        studentRepository.CreateAsync(Arg.Any<Student>(), Arg.Any<CancellationToken>())
            .Returns(call => { var s = call.Arg<Student>(); s.Id = store.StudentId; return Task.FromResult(s); });
        studentCareerRepository.CreateAsync(Arg.Any<StudentCareer>(), Arg.Any<CancellationToken>())
            .Returns(call => { var sc = call.Arg<StudentCareer>(); sc.Id = store.MembershipId; return Task.FromResult(sc); });
        // El punto clave: AssignStudyPlanAsync ESCRIBE en el store compartido.
        studentAcademicRepository.AssignStudyPlanAsync(Arg.Any<StudentStudyPlan>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var plan = call.Arg<StudentStudyPlan>();
                plan.IsCurrent = true;
                store.StudentStudyPlans.Add(plan);
                return Task.FromResult(plan);
            });
        unitOfWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<RegisterResult>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<RegisterResult>>>()(call.ArgAt<CancellationToken>(1)));

        return new RegisterUseCase(userRepository, studentRepository, studentCareerRepository,
            careerRepository, studyPlanRepository, studentAcademicRepository, unitOfWork);
    }

    private static CreateEnrollmentCommandHandler BuildEnrollHandler(FakeStore store)
    {
        var periodRepository = Substitute.For<IEnrollmentPeriodRepository>();
        var enrollmentRepository = Substitute.For<IEnrollmentRepository>();
        var studyPlanCourseRepository = Substitute.For<IStudyPlanCourseRepository>();
        var studentCareerRepository = Substitute.For<IStudentCareerRepository>();
        var studentAcademicRepository = Substitute.For<IStudentAcademicRepository>();
        var commissionRepository = Substitute.For<IDivisionRepository>();
        var courseSectionRepository = Substitute.For<ICourseSectionRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var period = new EnrollmentPeriod
        {
            Id = PeriodId, CareerId = CareerId, StudyPlanId = StudyPlanId, AcademicYear = 2026,
            Semester = 1, QuotasMorning = 10, QuotasAfternoon = 10, QuotasEvening = 10, IsActive = true
        };
        periodRepository.FindByIdAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(period));
        periodRepository.LockForEnrollmentAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(period));
        periodRepository.GetEnrolledShiftCountsAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((0, 0, 0)));
        studentCareerRepository.FindAsync(store.StudentId, CareerId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StudentCareer?>(new StudentCareer
            {
                Id = store.MembershipId, StudentId = store.StudentId, CareerId = CareerId, IsActive = true
            }));
        // El punto clave: la inscripción LEE el plan actual del MISMO store que escribió el registro.
        studentAcademicRepository.GetCurrentStudyPlanAsync(store.StudentId, CareerId, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(store.StudentStudyPlans.FirstOrDefault(p => p.IsCurrent)));
        studentAcademicRepository.GetPrerequisitesAsync(StudyPlanId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CoursePrerequisite>>([]));
        studentAcademicRepository.GetEnrollmentsAsync(store.StudentId, CareerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Enrollment>>([]));
        studentAcademicRepository.HasCurrentAcademicAssignmentAsync(store.MembershipId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));
        commissionRepository.FindMatchingActiveAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Division>>([]));
        courseSectionRepository.FindActiveByCourseTermAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<CourseSection>>([]));
        enrollmentRepository.GetByEnrollmentPeriodAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Enrollment>>([]));
        studyPlanCourseRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<StudyPlanCourse>>(
                [new StudyPlanCourse { Id = 101, CourseId = 1, StudyPlanId = StudyPlanId, YearNumber = 1 }]));
        enrollmentRepository.CreateAsync(Arg.Do<Enrollment>(store.CreatedEnrollments.Add), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<Enrollment>()));
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(call.ArgAt<CancellationToken>(1)));

        return new CreateEnrollmentCommandHandler(
            periodRepository, enrollmentRepository, studyPlanCourseRepository, studentCareerRepository,
            studentAcademicRepository, commissionRepository, courseSectionRepository,
            new EnrollmentEligibilityPolicy(new CourseEligibilityService()),
            new EnrollmentCapacityPolicy(), unitOfWork, TimeProvider.System);
    }

    private sealed class FakeStore
    {
        public long UserId { get; } = 11;
        public long StudentId { get; } = 6;
        public long MembershipId { get; } = 60;
        public List<StudentStudyPlan> StudentStudyPlans { get; } = [];
        public List<Enrollment> CreatedEnrollments { get; } = [];
    }
}
