using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using NSubstitute;
using Xunit;

namespace AcademiaDigital.Application.UnitTests.UseCases.Grades;

public sealed class ExamTableHandlersTests
{
    private static readonly DateTimeOffset Now = new(2027, 6, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Student_can_only_register_their_own_regularized_enrollment()
    {
        var repository = Substitute.For<IExamTableRepository>();
        var table = Table();
        var enrollment = Enrollment();
        enrollment.Student.UserId = 77;
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(table);
        repository.FindEnrollmentForUpdateAsync(30, Arg.Any<CancellationToken>()).Returns(enrollment);
        var handler = new RegisterForExamCommandHandler(
            repository, new ExamTablePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(
            new RegisterForExamCommand(10, 30, 88, false), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Tribunal_professor_is_required_to_record_results()
    {
        var repository = Substitute.For<IExamTableRepository>();
        repository.CanTeacherManageAsync(88, 10, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new SaveExamResultsCommandHandler(
            repository, new ExamTablePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new SaveExamResultsCommand(
            10, [new ExamResultInput(20, ExamResultOutcome.Passed, 8m, null)], 88, false),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Publishing_exam_applies_passed_result_atomically()
    {
        var repository = Substitute.For<IExamTableRepository>();
        var table = Table();
        table.Status = ExamTableStatus.Grading;
        table.Registrations =
        [
            new ExamRegistration
            {
                Id = 20,
                Enrollment = Enrollment(),
                GradeRevisions =
                [
                    new ExamGradeRevision
                    {
                        IsCurrent = true,
                        Outcome = ExamResultOutcome.Passed,
                        Grade = 8m
                    }
                ]
            }
        ];
        repository.FindForUpdateAsync(10, Arg.Any<CancellationToken>()).Returns(table);
        repository.FindAsync(10, Arg.Any<CancellationToken>()).Returns(table);
        var handler = new PublishExamTableCommandHandler(
            repository, new ExamTablePolicy(), new ImmediateUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new PublishExamTableCommand(10, 99), TestContext.Current.CancellationToken);

        Assert.Equal(ExamTableStatus.Published, result.Status);
        await repository.Received(1).PublishAsync(table, Arg.Any<CancellationToken>());
    }

    private static ExamTable Table() => new()
    {
        Id = 10,
        IdempotencyKey = "exam-table-request-001",
        CourseId = 2,
        Course = new Course { Id = 2, Code = "MAT", Name = "Mathematics" },
        AcademicYear = 2027,
        CallNumber = 1,
        ExamDateUtc = Now.UtcDateTime.AddDays(10),
        RegistrationDeadlineUtc = Now.UtcDateTime.AddDays(5),
        Location = "Room 1",
        Status = ExamTableStatus.Open
    };

    private static Enrollment Enrollment() => new()
    {
        Id = 30,
        CourseId = 2,
        StudentId = 40,
        Student = new Student
        {
            Id = 40,
            LegajoNumber = "LEG-40",
            User = new User { Id = 77, Username = "Ada", LastName = "Lovelace" }
        },
        Status = EnrollmentStatus.Regularized,
        StudyPlanCourse = new StudyPlanCourse
        {
            ApprovalRule = new CourseApprovalRule { MinimumFinalExamGrade = 6m }
        }
    };

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
