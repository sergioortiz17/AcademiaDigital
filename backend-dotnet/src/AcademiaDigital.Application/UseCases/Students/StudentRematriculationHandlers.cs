using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Exceptions;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Students;

public sealed record CreateStudentRematriculationCommand(
    long StudentId,
    int CareerId,
    int StudyPlanId,
    int CommissionId,
    int AcademicYear,
    int YearNumber,
    string? Notes,
    long CreatedByUserId);

public sealed record StudentRematriculationDto(
    long Id,
    long StudentId,
    long StudentCareerId,
    int CareerId,
    int StudyPlanId,
    string StudyPlanName,
    int CommissionId,
    string CommissionName,
    string Shift,
    int AcademicYear,
    int YearNumber,
    DateTime RematriculatedAt,
    long CreatedByUserId,
    string? Notes);

public sealed class CreateStudentRematriculationCommandHandler(
    IStudentRepository studentRepository,
    IStudyPlanRepository studyPlanRepository,
    ICommissionRepository commissionRepository,
    IRematriculationRepository rematriculationRepository,
    StudentRematriculationPolicy policy,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<StudentRematriculationDto> Handle(
        CreateStudentRematriculationCommand command,
        CancellationToken ct = default)
    {
        var student = await studentRepository.FindByIdAsync(command.StudentId, ct)
            ?? throw new KeyNotFoundException("Alumno no encontrado.");
        policy.ValidateStudent(student);
        var studyPlan = await studyPlanRepository.GetByIdAsync(command.StudyPlanId, ct)
            ?? throw new KeyNotFoundException("Plan de estudios no encontrado.");
        var commission = await commissionRepository.FindByIdAsync(command.CommissionId, ct)
            ?? throw new KeyNotFoundException("Comisión no encontrada.");
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return await unitOfWork.ExecuteInSerializableTransactionAsync(async transactionCt =>
        {
            var studentCareer = await rematriculationRepository.LockActiveStudentCareerAsync(
                student.Id, command.CareerId, transactionCt)
                ?? throw new InvalidOperationException("El alumno no está matriculado activamente en la carrera seleccionada.");
            policy.ValidateTarget(
                studentCareer,
                studyPlan,
                commission,
                command.AcademicYear,
                command.YearNumber);

            if (await rematriculationRepository.FindByCycleAsync(
                studentCareer.Id, command.AcademicYear, transactionCt) is not null)
                throw new StudentRematriculationAlreadyExistsException(
                    studentCareer.Id, command.AcademicYear);

            var latestAcademicYear = await rematriculationRepository.GetLatestAcademicYearAsync(
                studentCareer.Id, transactionCt);
            policy.ValidateNextCycle(latestAcademicYear, command.AcademicYear);

            var currentAssignments = await rematriculationRepository.GetCurrentAssignmentsAsync(
                studentCareer.Id, transactionCt);
            foreach (var current in currentAssignments)
            {
                current.IsCurrent = false;
                current.EndedAt = now;
            }

            var notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
            var assignment = new StudentAcademicAssignment
            {
                StudentId = student.Id,
                StudentCareerId = studentCareer.Id,
                CareerId = studentCareer.CareerId,
                StudyPlanId = studyPlan.Id,
                CommissionId = commission.Id,
                AcademicYear = command.AcademicYear,
                YearNumber = command.YearNumber,
                StartedAt = now,
                IsCurrent = true,
                Reason = notes ?? "Student rematriculation.",
                AssignedByUserId = command.CreatedByUserId
            };

            StudentStudyPlan? newStudyPlan = null;
            var currentStudyPlan = await rematriculationRepository.FindCurrentStudyPlanAsync(
                studentCareer.Id, transactionCt);
            if (currentStudyPlan?.StudyPlanId != studyPlan.Id)
            {
                if (currentStudyPlan is not null)
                {
                    currentStudyPlan.IsCurrent = false;
                    currentStudyPlan.EndedAt = now;
                }
                newStudyPlan = new StudentStudyPlan
                {
                    StudentId = student.Id,
                    StudentCareerId = studentCareer.Id,
                    StudyPlanId = studyPlan.Id,
                    IsCurrent = true,
                    AssignedAt = now,
                    MigrationReason = notes ?? "Study plan assigned during rematriculation."
                };
            }

            studentCareer.UpdatedAt = now;
            var rematriculation = new StudentRematriculation
            {
                StudentId = student.Id,
                StudentCareerId = studentCareer.Id,
                CareerId = studentCareer.CareerId,
                StudyPlanId = studyPlan.Id,
                CommissionId = commission.Id,
                AcademicYear = command.AcademicYear,
                YearNumber = command.YearNumber,
                RematriculatedAt = now,
                CreatedByUserId = command.CreatedByUserId,
                Notes = notes
            };

            var created = await rematriculationRepository.CreateAsync(
                rematriculation, assignment, newStudyPlan, transactionCt);
            return new StudentRematriculationDto(
                created.Id,
                created.StudentId,
                created.StudentCareerId,
                created.CareerId,
                created.StudyPlanId,
                studyPlan.Name,
                created.CommissionId,
                commission.Name,
                commission.Shift,
                created.AcademicYear,
                created.YearNumber,
                created.RematriculatedAt,
                created.CreatedByUserId,
                created.Notes);
        }, ct);
    }
}
