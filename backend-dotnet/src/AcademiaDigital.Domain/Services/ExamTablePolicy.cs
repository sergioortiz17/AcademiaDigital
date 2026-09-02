using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public sealed class ExamTablePolicy
{
    public void EnsureCanCreate(
        int academicYear,
        int callNumber,
        DateTime examDateUtc,
        DateTime registrationDeadlineUtc,
        string location,
        IReadOnlyCollection<ExamTribunalMember> tribunal,
        DateTime nowUtc)
    {
        if (academicYear != examDateUtc.Year)
            throw new ArgumentException("The exam date must belong to the selected academic year.");
        if (callNumber < 1 || callNumber > 10)
            throw new ArgumentException("The call number must be between one and ten.");
        if (examDateUtc < nowUtc)
            throw new ArgumentException("An exam table cannot be scheduled in the past.");
        if (registrationDeadlineUtc <= nowUtc || registrationDeadlineUtc > examDateUtc)
            throw new ArgumentException("The registration deadline must be in the future and no later than the exam date.");
        if (string.IsNullOrWhiteSpace(location) || location.Trim().Length > 200)
            throw new ArgumentException("A location of up to 200 characters is required.");
        if (tribunal.Count < 2 || tribunal.Count > 5)
            throw new ArgumentException("The tribunal requires between two and five teachers.");
        if (tribunal.Select(item => item.TeacherId).Distinct().Count() != tribunal.Count)
            throw new ArgumentException("A teacher cannot appear twice in the tribunal.");
        if (tribunal.Count(item => item.Role == ExamTribunalRole.President) != 1
            || !tribunal.Any(item => item.Role == ExamTribunalRole.Vocal))
            throw new ArgumentException("The tribunal requires exactly one president and at least one vocal.");
    }

    public void EnsureCanRegister(ExamTable table, Enrollment enrollment, DateTime nowUtc)
    {
        if (table.Status != ExamTableStatus.Open || nowUtc > table.RegistrationDeadlineUtc)
            throw new InvalidOperationException("Registration for this exam table is closed.");
        if (enrollment.CourseId != table.CourseId)
            throw new ArgumentException("The enrollment does not belong to the exam table course.");
        if (enrollment.Status != EnrollmentStatus.Regularized)
            throw new InvalidOperationException("Only a regularized enrollment can register for a final exam.");
    }

    public void EnsureCanStartGrading(ExamTable table)
    {
        if (table.Status != ExamTableStatus.Open)
            throw new InvalidOperationException("Only an open exam table can start grading.");
        if (table.Registrations.Count == 0)
            throw new InvalidOperationException("An exam table without registrations cannot start grading.");
    }

    public void EnsureCanRecordResults(ExamTable table)
    {
        if (table.Status != ExamTableStatus.Grading)
            throw new InvalidOperationException("Results can only be recorded while the exam table is grading.");
    }

    public void EnsureResultIsValid(ExamResultOutcome outcome, decimal? grade, decimal minimumPassingGrade)
    {
        if (minimumPassingGrade < 1m || minimumPassingGrade > 10m)
            throw new InvalidOperationException("The minimum final exam grade must be between one and ten.");
        if (outcome == ExamResultOutcome.Absent && grade.HasValue)
            throw new ArgumentException("An absent result cannot have a grade.");
        if (outcome != ExamResultOutcome.Absent && (!grade.HasValue || grade < 0m || grade > 10m))
            throw new ArgumentException("A present exam result requires a grade between zero and ten.");
        if (outcome == ExamResultOutcome.Passed && grade < minimumPassingGrade)
            throw new ArgumentException("A passing result must meet the minimum final exam grade.");
        if (outcome == ExamResultOutcome.Failed && grade >= minimumPassingGrade)
            throw new ArgumentException("A failing result must be below the minimum final exam grade.");
    }

    public void EnsureCanPublish(ExamTable table)
    {
        if (table.Status != ExamTableStatus.Grading)
            throw new InvalidOperationException("Only an exam table in grading can be published.");
        if (table.Registrations.Count == 0
            || table.Registrations.Any(registration => registration.GradeRevisions.All(revision => !revision.IsCurrent)))
            throw new InvalidOperationException("Every registration requires a result before publication.");
    }

    public void EnsureCanReopen(ExamTable table, string reason)
    {
        if (table.Status != ExamTableStatus.Published)
            throw new InvalidOperationException("Only a published exam table can be reopened.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 3 || reason.Trim().Length > 1000)
            throw new ArgumentException("A reopening reason between 3 and 1000 characters is required.");
    }
}
