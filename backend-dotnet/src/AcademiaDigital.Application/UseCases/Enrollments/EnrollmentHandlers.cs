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
            ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");

        if (!period.IsActive)
            throw new InvalidOperationException("El período de inscripción está cerrado.");

        var membership = await studentCareerRepository.FindAsync(command.StudentId, period.CareerId, true, ct)
            ?? throw new InvalidOperationException("El alumno no está matriculado activamente en la carrera del período de inscripción.");

        var currentStudyPlan = await studentAcademicRepository.GetCurrentStudyPlanAsync(
            command.StudentId, period.CareerId, ct)
            ?? throw new InvalidOperationException("El alumno no tiene un plan de estudios actual para la carrera del período de inscripción.");
        if (currentStudyPlan.StudyPlanId != period.StudyPlanId)
            throw new InvalidOperationException("El período de inscripción no coincide con el plan de estudios actual del alumno.");

        if (command.StudyPlanCourseIds.Count == 0)
            throw new ArgumentException("Debe seleccionarse al menos una materia.");

        var studyPlanCourses = await studyPlanCourseRepository.GetByIdsAsync(command.StudyPlanCourseIds, ct);
        if (studyPlanCourses.Count != command.StudyPlanCourseIds.Distinct().Count())
            throw new KeyNotFoundException("No se encontraron una o más materias del plan de estudios.");
        if (studyPlanCourses.Any(x => x.StudyPlanId != period.StudyPlanId))
            throw new InvalidOperationException("Todas las materias seleccionadas deben pertenecer al plan de estudios del período de inscripción.");

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
                ?? throw new KeyNotFoundException("Período de inscripción no encontrado.");

            if (!lockedPeriod.IsActive)
                throw new InvalidOperationException("El período de inscripción está cerrado.");

            var existing = await enrollmentRepository.GetByEnrollmentPeriodAsync(
                command.EnrollmentPeriodId,
                transactionCt);
            if (existing.Any(e => e.StudentId == command.StudentId))
                throw new InvalidOperationException("El alumno ya está inscripto en este período.");

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
