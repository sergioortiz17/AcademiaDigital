using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Application.Interfaces;

namespace AcademiaDigital.Application.UseCases.Enrollments;

public sealed record CreateEnrollmentCommand(
    long StudentId,
    int EnrollmentPeriodId,
    string Shift,
    IReadOnlyList<int> StudyPlanCourseIds);

public sealed class CreateEnrollmentCommandHandler(
    IEnrollmentPeriodRepository periodRepository,
    IEnrollmentRepository enrollmentRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    IStudentCareerRepository studentCareerRepository,
    IUnitOfWork unitOfWork)
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

        var membership = await studentCareerRepository.FindAsync(command.StudentId, period.CareerId, true, ct)
            ?? throw new InvalidOperationException("Student is not actively enrolled in the enrollment period career.");

        if (command.StudyPlanCourseIds.Count == 0)
            throw new ArgumentException("At least one course must be selected.");

        var existing = await enrollmentRepository.GetByEnrollmentPeriodAsync(command.EnrollmentPeriodId, ct);
        var alreadyEnrolled = existing.Any(e => e.StudentId == command.StudentId);
        if (alreadyEnrolled)
            throw new InvalidOperationException("Student is already enrolled in this period.");

        var studyPlanCourses = await studyPlanCourseRepository.GetByIdsAsync(command.StudyPlanCourseIds, ct);
        if (studyPlanCourses.Count != command.StudyPlanCourseIds.Distinct().Count())
            throw new KeyNotFoundException("One or more study plan courses were not found.");
        if (studyPlanCourses.Any(x => x.StudyPlanId != period.StudyPlanId))
            throw new InvalidOperationException("All selected courses must belong to the enrollment period study plan.");

        var enrollments = studyPlanCourses.Select(spc => new Enrollment
        {
            StudentId = command.StudentId,
            StudentCareerId = membership.Id,
            CourseId = spc.CourseId,
            StudyPlanCourseId = spc.Id,
            EnrollmentPeriodId = command.EnrollmentPeriodId,
            Shift = command.Shift,
            AcademicYear = period.AcademicYear,
            Semester = period.Semester,
            EnrollmentDate = DateTime.UtcNow,
            Status = EnrollmentStatus.Enrolled
        }).ToList();

        await unitOfWork.ExecuteInTransactionAsync(async transactionCt =>
        {
            foreach (var enrollment in enrollments)
                await enrollmentRepository.CreateAsync(enrollment, transactionCt);
            return true;
        }, ct);
    }
}
