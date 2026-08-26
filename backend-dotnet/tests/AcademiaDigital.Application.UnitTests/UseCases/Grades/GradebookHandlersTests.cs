using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Grades;

public sealed class GradebookHandlersTests
{
    private static readonly DateTimeOffset Now = new(2027, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_gradebook_uses_position_snapshot_and_idempotency_key()
    {
        var positions = Substitute.For<ITeachingPositionRepository>();
        var gradebooks = Substitute.For<IGradebookRepository>();
        positions.FindByIdAsync(5, Arg.Any<CancellationToken>()).Returns(Position());
        gradebooks.CreateIdempotentAsync(Arg.Any<Gradebook>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var gradebook = call.Arg<Gradebook>();
                gradebook.Id = 10;
                gradebook.Course = Course();
                gradebook.Commission = Commission();
                return (gradebook, true);
            });
        var handler = new CreateGradebookCommandHandler(
            positions, gradebooks, new GradebookPolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateGradebookCommand(
            "gradebook-request-001", 5,
            [new("Partial", 40m), new("Project", 60m)], 99, true),
            TestContext.Current.CancellationToken);

        Assert.Equal(10, result.Id);
        Assert.Equal(2, result.EvaluationCount);
        await gradebooks.Received(1).CreateIdempotentAsync(
            Arg.Is<Gradebook>(item => item.IdempotencyKey == "gradebook-request-001" && item.CourseId == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Professor_cannot_create_gradebook_outside_current_assignment()
    {
        var positions = Substitute.For<ITeachingPositionRepository>();
        var gradebooks = Substitute.For<IGradebookRepository>();
        positions.FindByIdAsync(5, Arg.Any<CancellationToken>()).Returns(Position());
        gradebooks.CanTeacherManagePositionAsync(88, 5, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateGradebookCommandHandler(
            positions, gradebooks, new GradebookPolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new CreateGradebookCommand(
            "gradebook-request-002", 5, [new("Final", 100m)], 88, false),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Bulk_grade_load_is_restricted_to_roster_and_evaluations()
    {
        var repository = Substitute.For<IGradebookRepository>();
        var gradebook = Gradebook();
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(gradebook);
        repository.GetRosterAsync(gradebook, Arg.Any<CancellationToken>()).Returns([Roster()]);
        var handler = new SaveGradeEntriesCommandHandler(
            repository, new GradebookPolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new SaveGradeEntriesCommand(
            10, [new GradeEntryInput(100, 999, 8m, null)], 99, true),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Close_gradebook_calculates_and_applies_enrollment_result()
    {
        var repository = Substitute.For<IGradebookRepository>();
        var gradebook = Gradebook();
        gradebook.Status = GradebookStatus.Published;
        var enrollment = Enrollment();
        gradebook.GradeRevisions =
        [
            Revision(gradebook.Evaluations.ElementAt(0), enrollment, 8m),
            Revision(gradebook.Evaluations.ElementAt(1), enrollment, 9m)
        ];
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(gradebook);
        repository.FindAsync(10, Arg.Any<CancellationToken>()).Returns(gradebook);
        var handler = new CloseGradebookCommandHandler(
            repository, new GradebookPolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CloseGradebookCommand(10, 99), TestContext.Current.CancellationToken);

        Assert.Equal(GradebookStatus.Closed, result.Status);
        await repository.Received(1).ApplyFinalResultsAsync(gradebook,
            Arg.Is<IReadOnlyList<EnrollmentGradebookResult>>(values =>
                values.Single().Average == 8.60m && values.Single().Status == EnrollmentStatus.Promoted),
            Arg.Any<CancellationToken>());
    }

    private static GradeEntryRevision Revision(GradebookEvaluation evaluation, Enrollment enrollment, decimal score)
        => new()
        {
            EvaluationId = evaluation.Id,
            Evaluation = evaluation,
            EnrollmentId = enrollment.Id,
            Enrollment = enrollment,
            IsCurrent = true,
            Score = score
        };

    private static Course Course() => new() { Id = 2, Code = "MAT", Name = "Mathematics" };
    private static Commission Commission() => new() { Id = 3, Code = "C1", Name = "Commission 1" };
    private static TeachingPosition Position() => new()
    {
        Id = 5, CourseId = 2, Course = Course(), CommissionId = 3, Commission = Commission(),
        AcademicYear = 2027, Semester = 1, IsActive = true
    };
    private static Enrollment Enrollment() => new()
    {
        Id = 30,
        StudyPlanCourse = new StudyPlanCourse
        {
            ApprovalRule = new CourseApprovalRule
            {
                MinimumRegularGrade = 6m,
                MinimumPromotionGrade = 8m,
                AllowsPromotion = true
            }
        }
    };
    private static Gradebook Gradebook() => new()
    {
        Id = 10,
        IdempotencyKey = "gradebook-request-001",
        TeachingPositionId = 5,
        CourseId = 2,
        Course = Course(),
        CommissionId = 3,
        Commission = Commission(),
        AcademicYear = 2027,
        Semester = 1,
        Status = GradebookStatus.Draft,
        Evaluations =
        [
            new GradebookEvaluation { Id = 100, Name = "Partial", WeightPercentage = 40m, MaximumScore = 10m, DisplayOrder = 1 },
            new GradebookEvaluation { Id = 101, Name = "Project", WeightPercentage = 60m, MaximumScore = 10m, DisplayOrder = 2 }
        ]
    };
    private static GradebookRosterRow Roster() => new(30, 40, "Ada Lovelace", "LEG-40", "12345678");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
        public Task<T> ExecuteInSerializableTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct = default) => operation(ct);
    }
}
