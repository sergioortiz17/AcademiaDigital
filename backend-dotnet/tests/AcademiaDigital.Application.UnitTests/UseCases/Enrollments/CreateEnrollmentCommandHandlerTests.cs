using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Enrollments;

public sealed class CreateEnrollmentCommandHandlerTests
{
    private const long StudentId = 42;
    private const long StudentCareerId = 420;
    private const int CareerId = 7;
    private const int StudyPlanId = 70;
    private const int PeriodId = 700;

    [Fact]
    public async Task Handle_rejects_an_invalid_shift_before_querying_repositories()
    {
        var context = CreateContext();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Handler.Handle(Command(shift: "Madrugada"), TestContext.Current.CancellationToken));

        Assert.Contains("Invalid shift", exception.Message);
        await context.PeriodRepository.DidNotReceive()
            .FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_rejects_a_closed_enrollment_period()
    {
        var period = ValidPeriod();
        period.IsActive = false;
        var context = CreateContext(period: period);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Equal("Enrollment period is closed.", exception.Message);
    }

    [Fact]
    public async Task Handle_requires_an_active_membership_in_the_period_career()
    {
        var context = CreateContext(hasActiveMembership: false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Equal("Student is not actively enrolled in the enrollment period career.", exception.Message);
    }

    [Fact]
    public async Task Handle_requires_at_least_one_selected_course()
    {
        var context = CreateContext();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            context.Handler.Handle(Command(courseIds: []), TestContext.Current.CancellationToken));

        Assert.Equal("At least one course must be selected.", exception.Message);
    }

    [Fact]
    public async Task Handle_rejects_a_student_already_enrolled_in_the_period()
    {
        var context = CreateContext(existingEnrollments:
        [
            new Enrollment { StudentId = StudentId, EnrollmentPeriodId = PeriodId }
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Equal("Student is already enrolled in this period.", exception.Message);
    }

    [Fact]
    public async Task Handle_rejects_when_the_selected_shift_has_no_vacancies()
    {
        var period = ValidPeriod();
        period.QuotasAfternoon = 1;
        var context = CreateContext(period: period, enrolledShiftCounts: (0, 1, 0));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Equal("No vacancies are available for shift 'Tarde'.", exception.Message);
        Assert.Empty(context.CreatedEnrollments);
    }

    [Fact]
    public async Task Handle_rejects_missing_study_plan_courses()
    {
        var context = CreateContext(studyPlanCourses:
        [
            PlanCourse(id: 101, courseId: 1)
        ]);

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            context.Handler.Handle(Command(courseIds: [101, 102]), TestContext.Current.CancellationToken));

        Assert.Equal("One or more study plan courses were not found.", exception.Message);
    }

    [Fact]
    public async Task Handle_rejects_courses_from_a_different_study_plan()
    {
        var context = CreateContext(studyPlanCourses:
        [
            PlanCourse(id: 101, courseId: 1, studyPlanId: StudyPlanId + 1)
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Equal("All selected courses must belong to the enrollment period study plan.", exception.Message);
    }

    [Fact]
    public async Task Handle_rejects_a_period_that_does_not_match_the_current_study_plan()
    {
        var context = CreateContext(currentStudyPlanId: StudyPlanId + 1);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Equal("The enrollment period does not match the student's current study plan.", exception.Message);
    }

    [Fact]
    public async Task Handle_rejects_courses_with_unsatisfied_strict_prerequisites()
    {
        var context = CreateContext(prerequisites:
        [
            DomainTestFactory.Prerequisite(courseId: 1, prerequisiteCourseId: 9, studyPlanId: StudyPlanId,
                type: PrerequisiteType.Strict, requiredStatus: MinimumRequiredStatus.Approved)
        ]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.Handler.Handle(Command(), TestContext.Current.CancellationToken));

        Assert.Contains("Strict prerequisites are not satisfied", exception.Message);
    }

    [Fact]
    public async Task Handle_allows_soft_prerequisites_as_warnings()
    {
        var context = CreateContext(prerequisites:
        [
            DomainTestFactory.Prerequisite(courseId: 1, prerequisiteCourseId: 9, studyPlanId: StudyPlanId,
                type: PrerequisiteType.Soft, requiredStatus: MinimumRequiredStatus.Approved)
        ]);

        await context.Handler.Handle(Command(), TestContext.Current.CancellationToken);

        Assert.Single(context.CreatedEnrollments);
    }

    [Fact]
    public async Task Handle_creates_every_enrollment_inside_one_transaction()
    {
        var context = CreateContext(studyPlanCourses:
        [
            PlanCourse(id: 101, courseId: 1),
            PlanCourse(id: 102, courseId: 2)
        ]);
        var before = DateTime.UtcNow;

        await context.Handler.Handle(
            Command(courseIds: [101, 102], shift: "Mañana"),
            TestContext.Current.CancellationToken);

        var after = DateTime.UtcNow;
        await context.UnitOfWork.Received(1).ExecuteInSerializableTransactionAsync(
            Arg.Any<Func<CancellationToken, Task<bool>>>(),
            Arg.Any<CancellationToken>());
        Assert.Collection(
            context.CreatedEnrollments.OrderBy(x => x.CourseId),
            first => AssertEnrollment(first, courseId: 1, studyPlanCourseId: 101, before, after),
            second => AssertEnrollment(second, courseId: 2, studyPlanCourseId: 102, before, after));
    }

    private static HandlerTestContext CreateContext(
        EnrollmentPeriod? period = null,
        bool hasActiveMembership = true,
        IReadOnlyList<Enrollment>? existingEnrollments = null,
        IReadOnlyList<StudyPlanCourse>? studyPlanCourses = null,
        int currentStudyPlanId = StudyPlanId,
        IReadOnlyList<CoursePrerequisite>? prerequisites = null,
        IReadOnlyList<Enrollment>? enrollmentHistory = null,
        (int Morning, int Afternoon, int Evening)? enrolledShiftCounts = null)
    {
        var periodRepository = Substitute.For<IEnrollmentPeriodRepository>();
        var enrollmentRepository = Substitute.For<IEnrollmentRepository>();
        var studyPlanCourseRepository = Substitute.For<IStudyPlanCourseRepository>();
        var studentCareerRepository = Substitute.For<IStudentCareerRepository>();
        var studentAcademicRepository = Substitute.For<IStudentAcademicRepository>();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var createdEnrollments = new List<Enrollment>();

        period ??= ValidPeriod();
        studyPlanCourses ??= [PlanCourse(id: 101, courseId: 1)];
        existingEnrollments ??= [];
        prerequisites ??= [];
        enrollmentHistory ??= [];

        periodRepository.FindByIdAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(period));
        periodRepository.LockForEnrollmentAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<EnrollmentPeriod?>(period));
        periodRepository.GetEnrolledShiftCountsAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(enrolledShiftCounts ?? (0, 0, 0)));
        studentCareerRepository.FindAsync(StudentId, CareerId, true, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StudentCareer?>(hasActiveMembership
                ? new StudentCareer { Id = StudentCareerId, StudentId = StudentId, CareerId = CareerId, IsActive = true }
                : null));
        studentAcademicRepository.GetCurrentStudyPlanAsync(StudentId, CareerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StudentStudyPlan?>(new StudentStudyPlan
            {
                StudentId = StudentId,
                StudentCareerId = StudentCareerId,
                StudyPlanId = currentStudyPlanId,
                IsCurrent = true
            }));
        studentAcademicRepository.GetPrerequisitesAsync(StudyPlanId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(prerequisites));
        studentAcademicRepository.GetEnrollmentsAsync(StudentId, CareerId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(enrollmentHistory));
        enrollmentRepository.GetByEnrollmentPeriodAsync(PeriodId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Enrollment>>(existingEnrollments));
        studyPlanCourseRepository.GetByIdsAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(studyPlanCourses));
        enrollmentRepository.CreateAsync(
                Arg.Do<Enrollment>(createdEnrollments.Add),
                Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.Arg<Enrollment>()));
        unitOfWork.ExecuteInSerializableTransactionAsync(
                Arg.Any<Func<CancellationToken, Task<bool>>>(),
                Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Func<CancellationToken, Task<bool>>>()(call.ArgAt<CancellationToken>(1)));

        var handler = new CreateEnrollmentCommandHandler(
            periodRepository,
            enrollmentRepository,
            studyPlanCourseRepository,
            studentCareerRepository,
            studentAcademicRepository,
            new EnrollmentEligibilityPolicy(new CourseEligibilityService()),
            new EnrollmentCapacityPolicy(),
            unitOfWork,
            TimeProvider.System);

        return new HandlerTestContext(handler, periodRepository, unitOfWork, createdEnrollments);
    }

    private static CreateEnrollmentCommand Command(
        IReadOnlyList<int>? courseIds = null,
        string shift = "Tarde")
        => new(StudentId, PeriodId, shift, courseIds ?? [101]);

    private static EnrollmentPeriod ValidPeriod()
        => new()
        {
            Id = PeriodId,
            CareerId = CareerId,
            StudyPlanId = StudyPlanId,
            AcademicYear = 2026,
            Semester = 2,
            QuotasMorning = 10,
            QuotasAfternoon = 10,
            QuotasEvening = 10,
            IsActive = true
        };

    private static StudyPlanCourse PlanCourse(int id, int courseId, int studyPlanId = StudyPlanId)
        => new() { Id = id, CourseId = courseId, StudyPlanId = studyPlanId };

    private static void AssertEnrollment(
        Enrollment enrollment,
        int courseId,
        int studyPlanCourseId,
        DateTime before,
        DateTime after)
    {
        Assert.Equal(StudentId, enrollment.StudentId);
        Assert.Equal(StudentCareerId, enrollment.StudentCareerId);
        Assert.Equal(courseId, enrollment.CourseId);
        Assert.Equal(studyPlanCourseId, enrollment.StudyPlanCourseId);
        Assert.Equal(PeriodId, enrollment.EnrollmentPeriodId);
        Assert.Equal("Mañana", enrollment.Shift);
        Assert.Equal(2026, enrollment.AcademicYear);
        Assert.Equal(2, enrollment.Semester);
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
        Assert.InRange(enrollment.EnrollmentDate, before, after);
    }

    private sealed record HandlerTestContext(
        CreateEnrollmentCommandHandler Handler,
        IEnrollmentPeriodRepository PeriodRepository,
        IUnitOfWork UnitOfWork,
        List<Enrollment> CreatedEnrollments);
}
