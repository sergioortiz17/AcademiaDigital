using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.DevTools;

/// <summary>
/// Escenario de prueba end-to-end "cursada con correlativa bloqueante" (ver README/plan).
///
/// Ejercita de VERDAD el control de correlativas contra los datos sembrados de DS2023,
/// reutilizando los MISMOS handlers que la API real (no rutas paralelas):
///  1. Inscribe un alumno de prueba (email/DNI fijo) en Programación I (C3) y Base de Datos (C4).
///  2. Base de Datos: cursada (gradebook completo -> Regularized) + final (mesa de examen
///     Passed -> Approved).  Programación I: gradebook con nota baja -> Failed.
///  3. Intenta inscribir al alumno en Programación II (C13), cuya correlativa Strict/Approved es
///     Programación I (+ Base de Datos). Como Prog I quedó Failed, DEBE bloquearse.
///  4. Reporta PASS (bloqueó por esa correlativa) o FAIL (no bloqueó / bloqueó por otra razón),
///     con el detalle paso a paso para diagnosticar si algo falla a mitad de camino.
///
/// La "simulación de tiempo" no toca el reloj: los Enrollment/EnrollmentPeriod usan el AcademicYear
/// que se elija (no se valida contra hoy). La ÚNICA parte atada al reloj real es la mesa de examen
/// (se agenda a futuro con año calendario actual), lo cual no impide completar todo en una corrida.
///
/// Idempotente: busca-o-crea por claves fijas, así se puede correr repetidas veces (y tras reset).
/// </summary>
public sealed class CorrelativaScenarioService(
    AppDbContext db,
    IUserRepository userRepository,
    IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository,
    IStudentAcademicRepository studentAcademicRepository,
    ITeacherRepository teacherRepository,
    ICareerRepository careerRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    IEnrollmentPeriodRepository enrollmentPeriodRepository,
    RegisterUseCase registerUseCase,
    CreateEnrollmentCommandHandler createEnrollment,
    CreateGradebookCommandHandler createGradebook,
    SaveGradeEntriesCommandHandler saveGrades,
    SubmitGradebookCommandHandler submitGradebook,
    ApproveGradebookCommandHandler approveGradebook,
    PublishGradebookCommandHandler publishGradebook,
    CloseGradebookCommandHandler closeGradebook,
    CreateExamTableCommandHandler createExamTable,
    RegisterForExamCommandHandler registerForExam,
    StartExamGradingCommandHandler startExamGrading,
    SaveExamResultsCommandHandler saveExamResults,
    PublishExamTableCommandHandler publishExamTable)
{
    private const string CareerCode = "DS2023";
    private const string ProgICode = "DS2023-03";   // Programación I  (queda Failed)
    private const string BaseDatosCode = "DS2023-04"; // Base de Datos (queda Approved)
    private const string ProgIICode = "DS2023-13";  // Programación II (intento bloqueado)
    private const string StudentEmail = "escenario.correlativa@test.local";
    private const string StudentDni = "99000001";
    private const string Shift = "Mañana";

    private readonly List<ScenarioStep> _steps = [];

    public async Task<ScenarioReport> RunAsync(CancellationToken ct = default)
    {
        try
        {
            // 1. Resolver carrera / plan / materias sembradas ------------------------------------
            var career = await Step("Resolver carrera DS2023",
                async () => await careerRepository.FindByCodeAsync(CareerCode, ct)
                    ?? throw new InvalidOperationException($"No existe la carrera {CareerCode}. ¿Corriste el reset+reseed?"),
                c => new { c.Id, c.Code, c.Name });

            var studyPlan = await Step("Resolver StudyPlan de la carrera",
                async () => (await studyPlanRepository.GetByCareerIdAsync(career.Id, ct)).FirstOrDefault()
                    ?? throw new InvalidOperationException("La carrera no tiene StudyPlan sembrado."),
                p => new { p.Id, p.Code, p.Name, Status = p.Status.ToString() });

            var planCourses = await studyPlanCourseRepository.GetByStudyPlanIdAsync(studyPlan.Id, ct);
            var progI = ResolveCourse(planCourses, ProgICode);
            var baseDatos = ResolveCourse(planCourses, BaseDatosCode);
            var progII = ResolveCourse(planCourses, ProgIICode);
            Record("Resolver materias C3/C4/C13", "ok", "Materias del plan resueltas por código.", new
            {
                progI = new { progI.Id, progI.Course.Code },
                baseDatos = new { baseDatos.Id, baseDatos.Course.Code },
                progII = new { progII.Id, progII.Course.Code }
            });

            // 2. Alumno de prueba (buscar-o-crear, idempotente) ---------------------------------
            var student = await Step("Buscar-o-crear alumno de prueba + inscripción en carrera + plan actual",
                async () => await EnsureStudentAsync(career, studyPlan, ct),
                s => new { s.Id, s.LegajoNumber, email = StudentEmail });

            // 3. Guard de repetibilidad: si el alumno de prueba ya tiene inscripciones (corrida
            //    previa sin reset), no seguir a medias — reportar claro que hay que resetear.
            var priorEnrollments = await db.Enrollments.AsNoTracking()
                .CountAsync(e => e.StudentId == student.Id, ct);
            if (priorEnrollments > 0)
            {
                Record("Verificar estado limpio del alumno de prueba", "fail",
                    $"El alumno de prueba ya tiene {priorEnrollments} inscripción(es) de una corrida anterior. " +
                    "Corré primero el reset+reseed de dev-tools y volvé a ejecutar el escenario.",
                    new { priorEnrollments });
                return Finish("FAIL",
                    "FAIL: el escenario ya se ejecutó antes (el alumno de prueba tiene inscripciones previas). " +
                    "Ejecutá el reset+reseed y volvé a correrlo — el escenario es repetible desde un estado limpio.");
            }
            Record("Verificar estado limpio del alumno de prueba", "ok", "Sin inscripciones previas.", new { priorEnrollments = 0 });

            // 4. Profesores del tribunal (2: presidente + vocal) --------------------------------
            var (teacherPresident, teacherVocal) = await Step("Buscar-o-crear 2 profesores para el tribunal",
                async () => (await EnsureTeacherAsync("PRES", ct), await EnsureTeacherAsync("VOC", ct)),
                t => new { presidentTeacherId = t.Item1.Id, vocalTeacherId = t.Item2.Id });

            var actorUserId = teacherPresident.UserId; // actor admin para las operaciones

            // 5. Período de inscripción 1er año + comisiones + cargos + inscripción en C3 y C4 ---
            var year1 = await Step("Crear período de inscripción de 1er año (activo)",
                async () => await CreateActivePeriodAsync(career.Id, studyPlan.Id, academicYear: 1, semester: 1, ct),
                p => new { p.Id, p.AcademicYear, p.IsActive });

            var (enrollC3, enrollC4) = await Step("Inscribir alumno en Programación I (C3) y Base de Datos (C4)",
                async () =>
                {
                    // Una sola inscripción con ambas materias: el handler rechaza 2 inscripciones
                    // del mismo alumno en el mismo período, por eso van juntas.
                    await createEnrollment.Handle(new CreateEnrollmentCommand(
                        student.Id, year1.Id, Shift, [progI.Id, baseDatos.Id]), ct);
                    var e3 = await FindEnrollment(student.Id, progI.CourseId, year1.Id, ct);
                    var e4 = await FindEnrollment(student.Id, baseDatos.CourseId, year1.Id, ct);
                    return (e3, e4);
                },
                e => new
                {
                    progI = new { enrollmentId = e.Item1.Id, course = ProgICode, status = e.Item1.Status.ToString() },
                    baseDatos = new { enrollmentId = e.Item2.Id, course = BaseDatosCode, status = e.Item2.Status.ToString() }
                });

            // 5. Base de Datos: cursada (Regularized) + final (Approved) -------------------------
            await Step("Base de Datos: gradebook completo con nota aprobada (-> Regularized)",
                async () => await RunGradebookAsync(baseDatos, career.Id, enrollC4, teacherPresident, actorUserId, score: 8m, ct),
                "gradebook cerrado");
            var c4AfterCursada = await ReloadEnrollment(enrollC4.Id, ct);
            Record("Verificar Base de Datos quedó Regularized tras la cursada", 
                c4AfterCursada.Status == EnrollmentStatus.Regularized ? "ok" : "warn",
                $"Estado tras cerrar gradebook: {c4AfterCursada.Status}.", new { status = c4AfterCursada.Status.ToString() });

            await Step("Base de Datos: mesa de examen final con resultado Passed (-> Approved)",
                async () => await RunFinalExamAsync(baseDatos, c4AfterCursada, teacherPresident, teacherVocal, actorUserId, ct),
                "mesa publicada");
            var c4Final = await ReloadEnrollment(enrollC4.Id, ct);
            Record("Verificar Base de Datos quedó Approved tras el final",
                c4Final.Status == EnrollmentStatus.Approved ? "ok" : "warn",
                $"Estado final Base de Datos: {c4Final.Status}.", new { status = c4Final.Status.ToString() });

            // 6. Programación I: gradebook con nota baja (-> Failed) -----------------------------
            await Step("Programación I: gradebook con nota baja (-> Failed)",
                async () => await RunGradebookAsync(progI, career.Id, enrollC3, teacherPresident, actorUserId, score: 3m, ct),
                "gradebook cerrado");
            var c3Final = await ReloadEnrollment(enrollC3.Id, ct);
            Record("Verificar Programación I quedó Failed (correlativa NO satisfecha)",
                c3Final.Status == EnrollmentStatus.Failed ? "ok" : "warn",
                $"Estado final Programación I: {c3Final.Status}.", new { status = c3Final.Status.ToString() });

            // 7. Período de inscripción 2º año -------------------------------------------------
            var year2 = await Step("Crear período de inscripción de 2º año (activo)",
                async () => await CreateActivePeriodAsync(career.Id, studyPlan.Id, academicYear: 2, semester: 1, ct),
                p => new { p.Id, p.AcademicYear, p.IsActive });

            // 8. Intento bloqueante: inscribir en Programación II (C13) --------------------------
            string? blockMessage = null;
            var blocked = false;
            try
            {
                await createEnrollment.Handle(new CreateEnrollmentCommand(
                    student.Id, year2.Id, Shift, [progII.Id]), ct);
            }
            catch (InvalidOperationException ex)
            {
                blocked = true;
                blockMessage = ex.Message;
            }

            // 9. Veredicto ----------------------------------------------------------------------
            var blockedByProgI = blocked
                && blockMessage!.Contains("Strict prerequisites are not satisfied", StringComparison.OrdinalIgnoreCase)
                && blockMessage.Contains(progI.CourseId.ToString());
            var baseDatosApproved = c4Final.Status == EnrollmentStatus.Approved;

            if (blockedByProgI && baseDatosApproved)
            {
                Record("Intento de inscripción a Programación II", "ok",
                    "Bloqueada correctamente por la correlativa Programación I (Base de Datos sí estaba aprobada).",
                    new { blocked, blockMessage });
                return Finish("PASS",
                    "PASS: la inscripción a Programación II fue bloqueada correctamente por la correlativa " +
                    "Programación I (sin aprobar), mientras que Base de Datos (aprobada) no bloqueó.");
            }

            // FAIL: distinguir el porqué
            string detail;
            if (!blocked)
                detail = "FAIL: la inscripción a Programación II NO fue bloqueada (se esperaba bloqueo por correlativa Programación I). Posible bug en el control de correlativas.";
            else if (!blockedByProgI)
                detail = $"FAIL: la inscripción se bloqueó, pero NO por la correlativa Programación I esperada. Mensaje real: {blockMessage}";
            else
                detail = $"FAIL: la inscripción se bloqueó por Programación I, pero Base de Datos no quedó Approved (estado: {c4Final.Status}), así que el escenario no es concluyente.";
            Record("Intento de inscripción a Programación II", "fail", detail, new { blocked, blockMessage, baseDatosStatus = c4Final.Status.ToString() });
            return Finish("FAIL", detail);
        }
        catch (Exception ex)
        {
            // Falla a mitad de camino: el último step quedó marcado, agregamos el error crudo.
            Record("Error inesperado", "fail", ex.Message, new { type = ex.GetType().Name });
            return Finish("FAIL", $"FAIL: el escenario se interrumpió con un error inesperado: {ex.Message}");
        }
    }

    // ── Helpers de dominio ──────────────────────────────────────────────────────────────────

    private static StudyPlanCourse ResolveCourse(IReadOnlyList<StudyPlanCourse> planCourses, string code)
        => planCourses.FirstOrDefault(spc => spc.Course.Code == code)
            ?? throw new InvalidOperationException($"No se encontró la materia {code} en el plan sembrado.");

    private async Task<Student> EnsureStudentAsync(Career career, StudyPlan studyPlan, CancellationToken ct)
    {
        var existingUser = await userRepository.FindByEmailAsync(StudentEmail, ct);
        Student student;
        if (existingUser is not null)
        {
            student = await studentRepository.FindByUserIdAsync(existingUser.Id, ct)
                ?? throw new InvalidOperationException("El usuario del escenario existe pero no tiene perfil de alumno.");
        }
        else
        {
            var result = await registerUseCase.ExecuteAsync(
                StudentEmail, "Escenario", "Correlativa", "Escenario123!", StudentDni, career.Id, ct);
            student = await studentRepository.FindByUserIdAsync(result.UserId, ct)
                ?? throw new InvalidOperationException("No se pudo crear el alumno del escenario.");
        }

        // Asegurar StudyPlan actual (RegisterUseCase no lo setea).
        var current = await studentAcademicRepository.GetCurrentStudyPlanAsync(student.Id, career.Id, ct);
        if (current is null || current.StudyPlanId != studyPlan.Id)
        {
            var membership = await studentCareerRepository.FindAsync(student.Id, career.Id, true, ct)
                ?? throw new InvalidOperationException("El alumno no tiene StudentCareer activa.");
            await studentAcademicRepository.AssignStudyPlanAsync(new StudentStudyPlan
            {
                StudentId = student.Id,
                StudentCareerId = membership.Id,
                StudyPlanId = studyPlan.Id,
                IsCurrent = true,
                AssignedAt = DateTime.UtcNow
            }, ct);
        }
        return student;
    }

    private async Task<Teacher> EnsureTeacherAsync(string suffix, CancellationToken ct)
    {
        var email = $"escenario.docente.{suffix.ToLowerInvariant()}@test.local";
        var user = await userRepository.FindByEmailAsync(email, ct);
        if (user is null)
            user = await userRepository.CreateAsync(email, $"Docente{suffix}", "Escenario", "Docente123!",
                $"9910{suffix.GetHashCode() & 0xFFFF:D4}"[..8], UserRole.Profesor, ct);

        var teacher = await teacherRepository.FindByUserIdAsync(user.Id, ct);
        if (teacher is null)
            teacher = await teacherRepository.CreateAsync(new Teacher
            {
                UserId = user.Id,
                EmployeeNumber = $"DOC-ESC-{suffix}",
                HireDate = DateTime.UtcNow.Date,
                IsActive = true
            }, ct);
        return teacher;
    }

    private async Task<EnrollmentPeriod> CreateActivePeriodAsync(int careerId, int studyPlanId, int academicYear, int semester, CancellationToken ct)
        => await enrollmentPeriodRepository.CreateAsync(new EnrollmentPeriod
        {
            CareerId = careerId,
            StudyPlanId = studyPlanId,
            AcademicYear = academicYear,
            Semester = semester,
            QuotasMorning = 50,
            QuotasAfternoon = 50,
            QuotasEvening = 50,
            IsActive = true,
            StartDate = DateTime.UtcNow
        }, ct);

    private async Task<Enrollment> FindEnrollment(long studentId, int courseId, int periodId, CancellationToken ct)
        => await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && e.CourseId == courseId && e.EnrollmentPeriodId == periodId)
            .OrderByDescending(e => e.Id).FirstAsync(ct);

    /// <summary>Crea una Commission + TeachingPosition (vía DbContext directo) y corre la cadena
    /// completa del gradebook hasta Close, dejando el enrollment Regularized (nota>=6) o Failed.</summary>
    private async Task RunGradebookAsync(StudyPlanCourse spc, int careerId, Enrollment enrollment, Teacher teacher, long actorUserId, decimal score, CancellationToken ct)
    {
        // La TeachingPosition/gradebook deben usar el MISMO AcademicYear/Semester que el enrollment:
        // el roster del gradebook matchea enrollments por CourseId+AcademicYear+Semester.
        var position = await EnsureTeachingPositionAsync(spc, careerId, teacher, enrollment.AcademicYear, enrollment.Semester, ct);

        // El enrollment que crea CreateEnrollmentCommandHandler no tiene TeachingPositionId; el roster
        // del gradebook incluye al alumno si su enrollment apunta a esa TeachingPosition. Se lo linkeamos.
        var trackedEnrollment = await db.Enrollments.FirstAsync(e => e.Id == enrollment.Id, ct);
        trackedEnrollment.TeachingPositionId = position.Id;
        await db.SaveChangesAsync(ct);

        var idem = $"gb-scn-{spc.CourseId}-{enrollment.Id}";
        var gb = await createGradebook.Handle(new CreateGradebookCommand(
            idem, position.Id,
            [new GradebookEvaluationInput("Final", 100m, 10m)],
            actorUserId, IsAdmin: true), ct);

        var detail = await saveGrades.Handle(new SaveGradeEntriesCommand(
            gb.Id,
            [new GradeEntryInput(await FirstEvaluationId(gb.Id, ct), enrollment.Id, score, null)],
            actorUserId, IsAdmin: true), ct);

        await submitGradebook.Handle(new SubmitGradebookCommand(gb.Id, actorUserId, IsAdmin: true), ct);
        await approveGradebook.Handle(new ApproveGradebookCommand(gb.Id, actorUserId), ct);
        await publishGradebook.Handle(new PublishGradebookCommand(gb.Id, actorUserId), ct);
        await closeGradebook.Handle(new CloseGradebookCommand(gb.Id, actorUserId), ct);
    }

    /// <summary>Corre la cadena de mesa de examen final hasta Publish (deja el enrollment Approved).</summary>
    private async Task RunFinalExamAsync(StudyPlanCourse spc, Enrollment enrollment, Teacher president, Teacher vocal, long actorUserId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var idem = $"exam-scn-{spc.CourseId}-{enrollment.Id}";
        var table = await createExamTable.Handle(new CreateExamTableCommand(
            idem, spc.CourseId, now.Year, CallNumber: 1,
            ExamDateUtc: now.AddDays(7), RegistrationDeadlineUtc: now.AddDays(1),
            Location: "Aula Escenario",
            Tribunal: [
                new ExamTribunalInput(president.Id, ExamTribunalRole.President),
                new ExamTribunalInput(vocal.Id, ExamTribunalRole.Vocal)
            ],
            actorUserId), ct);

        var registration = await registerForExam.Handle(new RegisterForExamCommand(table.Id, enrollment.Id, actorUserId, IsAdmin: true), ct);
        await startExamGrading.Handle(new StartExamGradingCommand(table.Id, actorUserId), ct);
        await saveExamResults.Handle(new SaveExamResultsCommand(
            table.Id,
            [new ExamResultInput(registration.Id, ExamResultOutcome.Passed, 8m, null)],
            actorUserId, IsAdmin: true), ct);
        await publishExamTable.Handle(new PublishExamTableCommand(table.Id, actorUserId), ct);
    }

    private async Task<TeachingPosition> EnsureTeachingPositionAsync(StudyPlanCourse spc, int careerId, Teacher teacher, int academicYear, int semester, CancellationToken ct)
    {
        // Commission (no hay repo Create -> DbContext directo). Buscar-o-crear por code fijo.
        var commissionCode = $"COM-SCN-{spc.CourseId}-{academicYear}";
        var commission = await db.Set<Commission>().FirstOrDefaultAsync(c => c.Code == commissionCode, ct);
        if (commission is null)
        {
            commission = new Commission
            {
                CareerId = careerId,
                Code = commissionCode,
                Name = $"Comisión escenario {spc.Course.Code}",
                AcademicYear = academicYear,
                YearNumber = spc.YearNumber,
                IsActive = true
            };
            db.Add(commission);
            await db.SaveChangesAsync(ct);
        }

        var position = await db.Set<TeachingPosition>()
            .FirstOrDefaultAsync(p => p.CourseId == spc.CourseId && p.CommissionId == commission.Id, ct);
        if (position is null)
        {
            var now = DateTime.UtcNow;
            position = new TeachingPosition
            {
                CourseId = spc.CourseId,
                CommissionId = commission.Id,
                AcademicYear = academicYear,
                Semester = semester,
                PositionType = PositionType.Titular,
                MaxStudents = 100,
                IsVacant = false,
                IsActive = true,
                TeacherId = teacher.Id,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Add(position);
            await db.SaveChangesAsync(ct);
        }
        return position;
    }

    private async Task<long> FirstEvaluationId(long gradebookId, CancellationToken ct)
        => await db.Set<GradebookEvaluation>().Where(e => e.GradebookId == gradebookId)
            .OrderBy(e => e.DisplayOrder).Select(e => e.Id).FirstAsync(ct);

    private async Task<Enrollment> ReloadEnrollment(long enrollmentId, CancellationToken ct)
        => await db.Enrollments.AsNoTracking().FirstAsync(e => e.Id == enrollmentId, ct);

    // ── Reporte paso a paso ─────────────────────────────────────────────────────────────────

    private async Task<T> Step<T>(string name, Func<Task<T>> action, Func<T, object> describe)
    {
        try
        {
            var result = await action();
            Record(name, "ok", "Completado.", describe(result));
            return result;
        }
        catch (Exception ex)
        {
            Record(name, "fail", ex.Message, new { type = ex.GetType().Name });
            throw; // propaga para cortar la secuencia; RunAsync captura y arma el reporte final
        }
    }

    /// <summary>Overload para pasos que no devuelven valor (acciones void).</summary>
    private async Task Step(string name, Func<Task> action, string okDetail)
    {
        try
        {
            await action();
            Record(name, "ok", okDetail);
        }
        catch (Exception ex)
        {
            Record(name, "fail", ex.Message, new { type = ex.GetType().Name });
            throw;
        }
    }

    private void Record(string step, string status, string detail, object? data = null)
        => _steps.Add(new ScenarioStep(_steps.Count + 1, step, status, detail, data));

    private ScenarioReport Finish(string result, string summary)
        => new(result, summary, _steps);
}

public sealed record ScenarioStep(int Order, string Step, string Status, string Detail, object? Data);
public sealed record ScenarioReport(string Result, string Summary, IReadOnlyList<ScenarioStep> Steps);
