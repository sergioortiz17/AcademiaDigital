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
        var positions = Substitute.For<ICourseSectionRepository>();
        var courses = Substitute.For<ICourseRepository>();
        var commissions = Substitute.For<IDivisionRepository>();
        var enrollments = Substitute.For<IEnrollmentRepository>();
        courses.FindByIdAsync(2, Arg.Any<CancellationToken>()).Returns(Course());
        commissions.FindByIdAsync(3, Arg.Any<CancellationToken>()).Returns(Division());
        positions.CreateAsync(Arg.Any<CourseSection>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var position = call.Arg<CourseSection>();
                position.Id = 5;
                return position;
            });
        positions.FindActiveByCourseTermAsync(
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<CourseSection>());
        var handler = new CreateCourseSectionCommandHandler(
            positions, courses, commissions, enrollments, new TeachingAssignmentPolicy(),
            SerializableUnitOfWork<CourseSection>(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateCourseSectionCommand(
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
        var positions = Substitute.For<ICourseSectionRepository>();
        var assignments = Substitute.For<ITeacherAssignmentRepository>();
        var teacherCareers = Substitute.For<ITeacherCareerRepository>();
        var unitOfWork = SerializableUnitOfWork();
        teachers.FindByIdAsync(4, Arg.Any<CancellationToken>()).Returns(Teacher());
        positions.FindByIdAsync(5, Arg.Any<CancellationToken>()).Returns(Position());
        assignments.AssignAsync(Arg.Any<TeacherAssignment>(), Arg.Any<CancellationToken>())
            .Returns(call => Assignment(call.Arg<TeacherAssignment>()));
        var handler = new AssignTeacherCommandHandler(
            teachers, positions, assignments, teacherCareers, new TeachingAssignmentPolicy(), unitOfWork,
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

    // ── Asignar varias materias de una división en un paso ──────────────────────────────

    private sealed class BulkFixture
    {
        public readonly ICourseSectionRepository Positions = Substitute.For<ICourseSectionRepository>();
        public readonly IStudyPlanRepository Plans = Substitute.For<IStudyPlanRepository>();
        public readonly IStudyPlanCourseRepository PlanCourses = Substitute.For<IStudyPlanCourseRepository>();
        public readonly List<CourseSection> Created = [];
        private readonly Dictionary<int, CourseSection> _byId = [];

        public BulkFixture(params CourseSection[] existing)
        {
            foreach (var section in existing) _byId[section.Id] = section;
            Positions.GetByCourseAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(call => _byId.Values.Where(s => s.CourseId == call.Arg<int>()).ToList());
            Positions.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(call => _byId.GetValueOrDefault(call.Arg<int>()));
            Positions.CreateAsync(Arg.Any<CourseSection>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var section = call.Arg<CourseSection>();
                section.Id = 100 + Created.Count;
                section.Course = Course(section.CourseId);
                section.Division = Division();
                Created.Add(section);
                _byId[section.Id] = section;
                return section;
            });
            Positions.FindActiveByCourseTermAsync(
                    Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(new List<CourseSection>());
            Plans.GetActiveByCareerIdAsync(10, Arg.Any<CancellationToken>())
                .Returns(DomainTestFactory.StudyPlan(id: 70, careerId: 10));
            PlanCourses.GetByStudyPlanIdAsync(70, Arg.Any<CancellationToken>()).Returns(new List<StudyPlanCourse>
            {
                new() { Id = 1, StudyPlanId = 70, CourseId = 2, YearNumber = 1, Semester = 1, IsActive = true },
                new() { Id = 2, StudyPlanId = 70, CourseId = 6, YearNumber = 1, Semester = 2, IsAnnual = true, IsActive = true },
                new() { Id = 3, StudyPlanId = 70, CourseId = 8, YearNumber = 1, Semester = 2, IsActive = true }
            });
        }

        public AssignCoursesToTeacherHandler Handler()
        {
            var teachers = Substitute.For<ITeacherRepository>();
            teachers.FindByIdAsync(4, Arg.Any<CancellationToken>()).Returns(Teacher());
            var divisions = Substitute.For<IDivisionRepository>();
            divisions.FindByIdAsync(3, Arg.Any<CancellationToken>()).Returns(Division());
            var courses = Substitute.For<ICourseRepository>();
            courses.FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(call => Course(call.Arg<int>()));
            var assignments = Substitute.For<ITeacherAssignmentRepository>();
            assignments.AssignAsync(Arg.Any<TeacherAssignment>(), Arg.Any<CancellationToken>()).Returns(call =>
            {
                var assignment = call.Arg<TeacherAssignment>();
                assignment.Teacher = Teacher();
                assignment.CourseSection = _byId[assignment.CourseSectionId];
                return assignment;
            });
            var policy = new TeachingAssignmentPolicy();
            var time = new FixedTimeProvider(Now);
            var create = new CreateCourseSectionCommandHandler(Positions, courses, divisions,
                Substitute.For<IEnrollmentRepository>(), policy, SerializableUnitOfWork<CourseSection>(), time);
            var assign = new AssignTeacherCommandHandler(teachers, Positions, assignments,
                Substitute.For<ITeacherCareerRepository>(), policy, SerializableUnitOfWork(), time);
            return new AssignCoursesToTeacherHandler(teachers, divisions, Plans, PlanCourses, Positions, create, assign);
        }
    }

    private static AssignCoursesToTeacherCommand BulkCommand(params int[] courseIds) => new(
        4, 3, courseIds, PositionType.Titular, new DateOnly(2027, 3, 1), 30, null, 99);

    [Fact]
    public async Task AssignCourses_reuses_a_vacant_section_and_creates_the_missing_ones_from_the_plan()
    {
        var fixture = new BulkFixture(Position()); // Comisión vacante existente de la materia 2.

        var result = await fixture.Handler().Handle(BulkCommand(2, 6), TestContext.Current.CancellationToken);

        Assert.Empty(result.Failed);
        Assert.Equal([5, 100], result.Assigned.Select(a => a.CourseSectionId));
        var created = Assert.Single(fixture.Created);
        Assert.Equal(6, created.CourseId);
        Assert.True(created.IsAnnual); // Anual según el plan de estudios.
        Assert.Equal(2027, created.AcademicYear);
    }

    [Fact]
    public async Task AssignCourses_reports_each_course_that_fails_without_stopping_the_rest()
    {
        var taken = Position();
        taken.IsVacant = false;
        taken.TeacherId = 77;
        var fixture = new BulkFixture(taken);

        var result = await fixture.Handler().Handle(BulkCommand(2, 8, 999), TestContext.Current.CancellationToken);

        Assert.Equal([8], result.Assigned.Select(a => a.CourseId));
        Assert.Contains("otro docente", result.Failed.Single(f => f.CourseId == 2).Reason);
        Assert.Contains("plan de estudios", result.Failed.Single(f => f.CourseId == 999).Reason);
    }

    [Fact]
    public async Task AssignCourses_rejects_a_start_date_outside_the_division_year_before_creating_anything()
    {
        var fixture = new BulkFixture();
        var command = BulkCommand(2) with { StartedOn = new DateOnly(2026, 12, 1) };

        await Assert.ThrowsAsync<ArgumentException>(
            () => fixture.Handler().Handle(command, TestContext.Current.CancellationToken));
        Assert.Empty(fixture.Created);
    }

    private static IUnitOfWork SerializableUnitOfWork() => SerializableUnitOfWork<TeacherAssignment>();

    private static IUnitOfWork SerializableUnitOfWork<T>()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<T>>>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<T>>>()(
                call.ArgAt<CancellationToken>(1)));
        return unitOfWork;
    }

    private static Course Course(int id = 2) => new()
    {
        Id = id, CareerId = 10, Code = "ARQ" + id, Name = "Materia " + id, IsActive = true
    };
    private static Division Division() => new()
    {
        Id = 3, CareerId = 10, Code = "C1", Name = "Comisión 1", AcademicYear = 2027, IsActive = true
    };
    private static User ProfessorUser() => new()
    {
        Id = 88, Username = "Ada", LastName = "Lovelace", IsActive = true, Role = UserRole.Profesor
    };
    private static Teacher Teacher() => new() { Id = 4, IsActive = true, UserId = 88, User = ProfessorUser() };
    private static CourseSection Position() => new()
    {
        Id = 5,
        CourseId = 2,
        Course = Course(),
        DivisionId = 3,
        Division = Division(),
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
            CourseSectionId = 5,
            StartedOn = new DateOnly(2027, 3, 1),
            IsCurrent = true,
            CreatedAt = Now.UtcDateTime
        };
        assignment.Id = 7;
        assignment.Teacher = Teacher();
        assignment.CourseSection = Position();
        return assignment;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
