using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Services;
using Xunit;

namespace AcademiaDigital.Domain.UnitTests.Services;

public sealed class ExamTablePolicyTests
{
    private readonly ExamTablePolicy policy = new();
    private static readonly DateTime Now = new(2027, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Tribunal_requires_one_president_and_at_least_one_vocal()
    {
        policy.EnsureCanCreate(2027, 1, Now.AddDays(10), Now.AddDays(5), "Room 1", new[]
        {
            Member(1, ExamTribunalRole.President),
            Member(2, ExamTribunalRole.Vocal)
        }, Now);
        Assert.Throws<ArgumentException>(() => policy.EnsureCanCreate(
            2027, 1, Now.AddDays(10), Now.AddDays(5), "Room 1",
            new[] { Member(1, ExamTribunalRole.President), Member(2, ExamTribunalRole.President) }, Now));
    }

    [Fact]
    public void Only_regularized_enrollment_can_register_before_deadline()
    {
        var table = Table();
        var enrollment = new Enrollment { CourseId = 3, Status = EnrollmentStatus.Regularized };
        policy.EnsureCanRegister(table, enrollment, Now);
        enrollment.Status = EnrollmentStatus.Enrolled;
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanRegister(table, enrollment, Now));
    }

    [Fact]
    public void Result_outcome_must_match_the_passing_threshold()
    {
        policy.EnsureResultIsValid(ExamResultOutcome.Passed, 6m, 6m);
        policy.EnsureResultIsValid(ExamResultOutcome.Failed, 5.99m, 6m);
        policy.EnsureResultIsValid(ExamResultOutcome.Absent, null, 6m);
        Assert.Throws<ArgumentException>(() => policy.EnsureResultIsValid(ExamResultOutcome.Passed, 5m, 6m));
        Assert.Throws<ArgumentException>(() => policy.EnsureResultIsValid(ExamResultOutcome.Absent, 0m, 6m));
    }

    [Fact]
    public void Publication_requires_a_current_result_for_every_registration()
    {
        var table = Table();
        table.Status = ExamTableStatus.Grading;
        table.Registrations = [new ExamRegistration { GradeRevisions = [] }];
        Assert.Throws<InvalidOperationException>(() => policy.EnsureCanPublish(table));
        table.Registrations.Single().GradeRevisions.Add(new ExamGradeRevision { IsCurrent = true });
        policy.EnsureCanPublish(table);
    }

    private static ExamTable Table() => new()
    {
        CourseId = 3,
        Status = ExamTableStatus.Open,
        RegistrationDeadlineUtc = Now.AddDays(5)
    };

    private static ExamTribunalMember Member(long teacherId, ExamTribunalRole role)
        => new() { TeacherId = teacherId, Role = role };
}
