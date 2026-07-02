using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;

namespace AcademiaDigital.Application.UseCases.Enrollments;

public sealed record CreateEnrollmentCommand(
    long StudentId,
    int EnrollmentPeriodId,
    string Shift,
    IReadOnlyList<int> StudyPlanCourseIds);

public sealed class CreateEnrollmentCommandHandler(
    IEnrollmentPeriodRepository periodRepository,
    IEnrollmentRepository enrollmentRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository)
{
    private static readonly HashSet<string> ValidShifts = ["Mañana", "Tarde", "Noche"];

    public async Task Handle(CreateEnrollmentCommand command, CancellationToken ct = default)
    {
        if (!ValidShifts.Contains(command.Shift))
            throw new ArgumentException($"Invalid shift '{command.Shift}'. Valid values: Tarde, Noche.");

        var period = await periodRepository.FindByIdAsync(command.EnrollmentPeriodId, ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");

        if (!period.IsActive)
            throw new InvalidOperationException("Enrollment period is closed.");

        if (command.StudyPlanCourseIds.Count == 0)
            throw new ArgumentException("At least one course must be selected.");

        var existing = await enrollmentRepository.GetByEnrollmentPeriodAsync(command.EnrollmentPeriodId, ct);
        var alreadyEnrolled = existing.Any(e => e.StudentId == command.StudentId);
        if (alreadyEnrolled)
            throw new InvalidOperationException("Student is already enrolled in this period.");

        var studyPlanCourses = await studyPlanCourseRepository.GetByIdsAsync(command.StudyPlanCourseIds, ct);

        var enrollments = studyPlanCourses.Select(spc => new Enrollment
        {
            StudentId = command.StudentId,
            CourseId = spc.CourseId,
            StudyPlanCourseId = spc.Id,
            EnrollmentPeriodId = command.EnrollmentPeriodId,
            Shift = command.Shift,
            AcademicYear = period.AcademicYear,
            Semester = period.Semester,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        }).ToList();

        foreach (var enrollment in enrollments)
            await enrollmentRepository.CreateAsync(enrollment, ct);
    }
}
