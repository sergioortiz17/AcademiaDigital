using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Services;

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
    IStudentAcademicRepository studentAcademicRepository,
    EnrollmentEligibilityPolicy eligibilityPolicy,
    EnrollmentCapacityPolicy capacityPolicy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task Handle(CreateEnrollmentCommand command, CancellationToken ct = default)
    {
        capacityPolicy.EnsureValidShift(command.Shift);

        var period = await periodRepository.FindByIdAsync(command.EnrollmentPeriodId, ct)
            ?? throw new KeyNotFoundException("Enrollment period not found.");

        if (!period.IsActive)
            throw new InvalidOperationException("Enrollment period is closed.");

        var membership = await studentCareerRepository.FindAsync(command.StudentId, period.CareerId, true, ct)
            ?? throw new InvalidOperationException("Student is not actively enrolled in the enrollment period career.");

        var currentStudyPlan = await studentAcademicRepository.GetCurrentStudyPlanAsync(
            command.StudentId, period.CareerId, ct)
            ?? throw new InvalidOperationException("Student has no current study plan for the enrollment period career.");
        if (currentStudyPlan.StudyPlanId != period.StudyPlanId)
            throw new InvalidOperationException("The enrollment period does not match the student's current study plan.");

        if (command.StudyPlanCourseIds.Count == 0)
            throw new ArgumentException("At least one course must be selected.");

        var studyPlanCourses = await studyPlanCourseRepository.GetByIdsAsync(command.StudyPlanCourseIds, ct);
        if (studyPlanCourses.Count != command.StudyPlanCourseIds.Distinct().Count())
            throw new KeyNotFoundException("One or more study plan courses were not found.");
        if (studyPlanCourses.Any(x => x.StudyPlanId != period.StudyPlanId))
            throw new InvalidOperationException("All selected courses must belong to the enrollment period study plan.");

        var prerequisites = await studentAcademicRepository.GetPrerequisitesAsync(period.StudyPlanId, ct);
        var enrollmentHistory = await studentAcademicRepository.GetEnrollmentsAsync(
            command.StudentId, period.CareerId, ct);
        eligibilityPolicy.EnsureCanEnroll(studyPlanCourses, prerequisites, enrollmentHistory);

        var now = timeProvider.GetUtcNow().UtcDateTime;

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
            EnrollmentDate = now,
            Status = EnrollmentStatus.Enrolled
        }).ToList();

        await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var lockedPeriod = await periodRepository.LockForEnrollmentAsync(
                command.EnrollmentPeriodId,
                transactionCt)
                ?? throw new KeyNotFoundException("Enrollment period not found.");

            if (!lockedPeriod.IsActive)
                throw new InvalidOperationException("Enrollment period is closed.");

            var existing = await enrollmentRepository.GetByEnrollmentPeriodAsync(
                command.EnrollmentPeriodId,
                transactionCt);
            if (existing.Any(e => e.StudentId == command.StudentId))
                throw new InvalidOperationException("Student is already enrolled in this period.");

            var counts = await periodRepository.GetEnrolledShiftCountsAsync(
                command.EnrollmentPeriodId,
                transactionCt);
            capacityPolicy.EnsureVacancy(lockedPeriod, counts, command.Shift);

            foreach (var enrollment in enrollments)
                await enrollmentRepository.CreateAsync(enrollment, transactionCt);
            return true;
        }, ct);
    }
}
