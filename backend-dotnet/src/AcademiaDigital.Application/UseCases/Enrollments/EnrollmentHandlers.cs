using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Domain.Services;

namespace AcademiaDigital.Application.UseCases.Enrollments;

public sealed record CreateEnrollmentCommand(
    long StudentId,
    int EnrollmentPeriodId,
    string Shift,
    IReadOnlyList<int> StudyPlanCourseIds,
    long ActorUserId);

public sealed class CreateEnrollmentCommandHandler(
    IEnrollmentPeriodRepository periodRepository,
    IEnrollmentRepository enrollmentRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    IStudentCareerRepository studentCareerRepository,
    IStudentAcademicRepository studentAcademicRepository,
    IDivisionRepository commissionRepository,
    ICourseSectionRepository courseSectionRepository,
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

        // Auto-match NIVEL 2 (por materia): para cada materia inscripta, buscar su CourseSection
        // (sección de esa materia en este ciclo/cuatrimestre) SIN filtrar por división del alumno.
        // Esto vincula al recursante — que cursa una materia fuera de su división — a la sección
        // correcta. Si hay exactamente una sección activa, se linkea; 0 o 2+ → queda sin sección
        // (manual), sin bloquear la inscripción.
        var sectionByStudyPlanCourseId = new Dictionary<int, int?>();
        foreach (var spc in studyPlanCourses)
        {
            var sections = await courseSectionRepository.FindActiveByCourseTermAsync(
                spc.CourseId, period.AcademicYear, period.Semester, spc.IsAnnual, ct);
            sectionByStudyPlanCourseId[spc.Id] = sections.Count == 1 ? sections[0].Id : null;
        }

        var enrollments = studyPlanCourses.Select(spc => new Enrollment
        {
            StudentId = command.StudentId,
            StudentCareerId = membership.Id,
            CourseId = spc.CourseId,
            StudyPlanCourseId = spc.Id,
            CourseSectionId = sectionByStudyPlanCourseId[spc.Id],
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

            await TryAutoAssignDivisionAsync(command, period, membership, studyPlanCourses, now, transactionCt);
            return true;
        }, ct);
    }

    /// <summary>
    /// Asignación automática de comisión al inscribirse (caso común, mínima intervención del admin).
    ///
    /// Criterio de "coincidencia clara" (si no se cumple, NO se asigna y la inscripción igual queda
    /// hecha, para que un admin la resuelva a mano — comportamiento idéntico al de antes):
    ///  - Todas las materias inscriptas son del MISMO año del plan (YearNumber). Si abarcan varios
    ///    años → ambiguo (una comisión es por un año), no se asigna.
    ///  - Existe EXACTAMENTE UNA comisión activa que matchea Carrera + Año académico + ese YearNumber
    ///    + el mismo Turno elegido. Cero o 2+ → ambiguo, no se asigna.
    ///  - El alumno no tiene ya una asignación vigente para esta membresía (no pisar una manual).
    ///
    /// El Shift se compara directo porque Division.Shift y Enrollment.Shift ya están unificados en
    /// español (Mañana/Tarde/Noche) — ver migración NormalizeCommissionShiftToSpanish.
    /// </summary>
    private async Task TryAutoAssignDivisionAsync(
        CreateEnrollmentCommand command,
        EnrollmentPeriod period,
        StudentCareer membership,
        IReadOnlyList<StudyPlanCourse> studyPlanCourses,
        DateTime now,
        CancellationToken ct)
    {
        var distinctYears = studyPlanCourses.Select(spc => spc.YearNumber).Distinct().ToList();
        if (distinctYears.Count != 1)
            return; // materias de varios años → ambiguo

        var yearNumber = distinctYears[0];

        if (await studentAcademicRepository.HasCurrentAcademicAssignmentAsync(membership.Id, ct))
            return; // ya tiene una asignación vigente (p. ej. manual, Parte 2)

        var matches = await commissionRepository.FindMatchingActiveAsync(
            period.CareerId, period.AcademicYear, yearNumber, command.Shift, ct);
        if (matches.Count != 1)
            return; // 0 o 2+ → ambiguo, lo resuelve el admin a mano

        var commission = matches[0];
        await studentAcademicRepository.AddAcademicAssignmentAsync(new StudentAcademicAssignment
        {
            StudentId = command.StudentId,
            StudentCareerId = membership.Id,
            CareerId = period.CareerId,
            StudyPlanId = period.StudyPlanId,
            DivisionId = commission.Id,
            AcademicYear = period.AcademicYear,
            YearNumber = yearNumber,
            IsCurrent = true,
            StartedAt = now,
            Reason = "Asignación automática al inscribirse (turno coincidente, comisión única).",
            AssignedByUserId = command.ActorUserId
        }, ct);
    }
}
