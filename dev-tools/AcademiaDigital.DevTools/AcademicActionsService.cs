using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.DevTools;

/// <summary>
/// Acciones académicas SUELTAS y parametrizadas para armar casos de prueba ad-hoc desde dev-tools.
/// Son las mismas piezas ya probadas en CorrelativaScenarioService (el escenario fijo), pero
/// reutilizables para CUALQUIER alumno/carrera/materia/año, reutilizando los MISMOS handlers reales.
///
/// Opción A: la Commission y la TeachingPosition necesarias para notas/mesa se auto-crean o reusan
/// detrás de escena (con un profesor de prueba), y cada acción reporta qué comisión/cargo usó, para
/// que no sea una caja negra al diagnosticar.
///
/// NO toca el escenario fijo (/api/scenario/correlativa-bloqueante), que sigue como test de regresión.
/// </summary>
public sealed class AcademicActionsService(
    AppDbContext db,
    IUserRepository userRepository,
    IStudentRepository studentRepository,
    IStudentCareerRepository studentCareerRepository,
    IStudentAcademicRepository studentAcademicRepository,
    ICareerRepository careerRepository,
    IStudyPlanRepository studyPlanRepository,
    IStudyPlanCourseRepository studyPlanCourseRepository,
    ITeacherRepository teacherRepository,
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
    PublishExamTableCommandHandler publishExamTable,
    GetStudentAcademicProgressQueryHandler academicProgress,
    GetEligibleCoursesForStudentQueryHandler eligibleCourses,
    AcademiaDigital.Application.UseCases.Teachers.AssignTeacherCommandHandler assignTeacher,
    ICourseRepository courseRepository)
{
    private const string Shift = "Mañana";

    // ── 1. Alumnos ───────────────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<object>> ListStudentsAsync(int? careerId, CancellationToken ct)
    {
        var students = careerId is > 0
            ? await studentRepository.GetByCareerAsync(careerId.Value, ct)
            : await studentRepository.GetAllAsync(ct);
        // Cargamos User/Career para mostrar nombre; GetAll ya los incluye en el repo real.
        return students.Select(s => (object)new
        {
            studentId = s.Id,
            legajo = s.LegajoNumber,
            careerId = s.CareerId,
            name = (s.User?.Username + " " + s.User?.LastName)?.Trim(),
            email = s.User?.Email,
            status = s.Status.ToString()
        }).ToList();
    }

    public async Task<object> CreateStudentAsync(string name, string lastName, string email, string password, string dni, int careerId, CancellationToken ct)
    {
        var career = await careerRepository.FindByIdAsync(careerId, ct)
            ?? throw new InvalidOperationException("Carrera no encontrada.");
        var result = await registerUseCase.ExecuteAsync(email, name, lastName, password, dni, career.Id, ct);
        var student = await studentRepository.FindByUserIdAsync(result.UserId, ct)!;

        // Asegurar StudyPlan actual (RegisterUseCase no lo setea) — necesario para inscribir después.
        var plan = (await studyPlanRepository.GetByCareerIdAsync(career.Id, ct)).FirstOrDefault();
        if (plan is not null)
        {
            var membership = await studentCareerRepository.FindAsync(student!.Id, career.Id, true, ct);
            if (membership is not null && await studentAcademicRepository.GetCurrentStudyPlanAsync(student.Id, career.Id, ct) is null)
                await studentAcademicRepository.AssignStudyPlanAsync(new StudentStudyPlan
                {
                    StudentId = student.Id,
                    StudentCareerId = membership.Id,
                    StudyPlanId = plan.Id,
                    IsCurrent = true,
                    AssignedAt = DateTime.UtcNow
                }, ct);
        }
        return new { studentId = student!.Id, userId = result.UserId, careerId = career.Id, currentStudyPlanId = plan?.Id };
    }

    // ── 2. Inscribir en materias ─────────────────────────────────────────────────────────────

    public async Task<ActionResult> EnrollAsync(long studentId, int studyPlanCourseId, int academicYear, int semester, CancellationToken ct)
        => await EnrollManyAsync(studentId, [studyPlanCourseId], academicYear, semester, ct);

    public async Task<ActionResult> EnrollManyAsync(long studentId, IReadOnlyList<int> studyPlanCourseIds, int academicYear, int semester, CancellationToken ct)
    {
        var steps = new List<object>();
        var student = await studentRepository.FindByIdAsync(studentId, ct)
            ?? throw new InvalidOperationException("Alumno no encontrado.");
        var spcs = await studyPlanCourseRepository.GetByIdsAsync(studyPlanCourseIds, ct);
        if (spcs.Count == 0) throw new InvalidOperationException("No se encontraron las materias del plan indicadas.");
        var studyPlanId = spcs[0].StudyPlanId;

        var period = await ResolveOrCreatePeriodAsync(student.CareerId, studyPlanId, academicYear, semester, steps, ct);

        await createEnrollment.Handle(new CreateEnrollmentCommand(studentId, period.Id, Shift,
            spcs.Select(s => s.Id).ToList()), ct);

        var enrolled = new List<object>();
        foreach (var spc in spcs)
        {
            var e = await db.Enrollments.AsNoTracking()
                .Where(x => x.StudentId == studentId && x.CourseId == spc.CourseId && x.EnrollmentPeriodId == period.Id)
                .OrderByDescending(x => x.Id).FirstAsync(ct);
            enrolled.Add(new { enrollmentId = e.Id, courseId = spc.CourseId, status = e.Status.ToString() });
        }
        steps.Add(new { step = "Inscripción", status = "ok", detail = $"Alumno inscripto en {spcs.Count} materia(s) (período {period.AcademicYear}/{period.Semester}).", data = enrolled });
        return new ActionResult(true, "Inscripción completada.", steps);
    }

    // ── 3. Notas (cadena Gradebook completa) ─────────────────────────────────────────────────

    public async Task<ActionResult> RunGradebookAsync(long enrollmentId, decimal score, long? teacherId, CancellationToken ct)
    {
        var steps = new List<object>();
        var enrollment = await db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new InvalidOperationException("Enrollment no encontrado.");
        var spc = await ResolveStudyPlanCourseForEnrollment(enrollment, ct);
        // Profesor real si se indica; si no, un docente de prueba (comportamiento previo).
        var teacher = teacherId is > 0
            ? await teacherRepository.FindByIdAsync(teacherId.Value, ct)
                ?? throw new InvalidOperationException("Docente no encontrado.")
            : await EnsureTestTeacherAsync("A", ct);
        steps.Add(new { step = "Profesor de la planilla", status = "ok",
            detail = teacherId is > 0 ? $"Profesor real id {teacher.Id}." : $"Docente de prueba id {teacher.Id} (no se indicó profesor).",
            data = new { teacherId = teacher.Id, isTestTeacher = teacherId is not > 0 } });

        var position = await ResolveOrCreateTeachingPositionAsync(spc, enrollment.CourseId, enrollment.AcademicYear, enrollment.Semester, enrollment.StudentId, teacher, steps, ct);

        // Linkear el enrollment a la TeachingPosition (el roster del gradebook lo requiere).
        var tracked = await db.Enrollments.FirstAsync(e => e.Id == enrollmentId, ct);
        if (tracked.TeachingPositionId != position.Id)
        {
            tracked.TeachingPositionId = position.Id;
            await db.SaveChangesAsync(ct);
            steps.Add(new { step = "Vincular enrollment ↔ cargo", status = "ok", detail = $"enrollment {enrollmentId} → teachingPosition {position.Id}." });
        }

        var idem = $"gb-adhoc-{enrollment.CourseId}-{enrollmentId}";
        var gb = await createGradebook.Handle(new CreateGradebookCommand(
            idem, position.Id, [new GradebookEvaluationInput("Final", 100m, 10m)], teacher.UserId, IsAdmin: true), ct);
        steps.Add(new { step = "Crear gradebook", status = "ok", detail = $"gradebook {gb.Id} (Draft).", data = new { gb.Id } });

        await saveGrades.Handle(new SaveGradeEntriesCommand(gb.Id,
            [new GradeEntryInput(await FirstEvaluationId(gb.Id, ct), enrollmentId, score, null)], teacher.UserId, IsAdmin: true), ct);
        await submitGradebook.Handle(new SubmitGradebookCommand(gb.Id, teacher.UserId, IsAdmin: true), ct);
        await approveGradebook.Handle(new ApproveGradebookCommand(gb.Id, teacher.UserId), ct);
        await publishGradebook.Handle(new PublishGradebookCommand(gb.Id, teacher.UserId), ct);
        await closeGradebook.Handle(new CloseGradebookCommand(gb.Id, teacher.UserId), ct);

        var final = await db.Enrollments.AsNoTracking().FirstAsync(e => e.Id == enrollmentId, ct);
        steps.Add(new { step = "Cerrar gradebook (Draft→…→Closed)", status = "ok",
            detail = $"Nota {score}. Estado final del enrollment: {final.Status} (nota {final.FinalGrade}).",
            data = new { status = final.Status.ToString(), finalGrade = final.FinalGrade } });

        return new ActionResult(true, $"Gradebook cerrado. Estado: {final.Status}.", steps);
    }

    // ── 4. Mesa de examen (Regularized → Approved) ───────────────────────────────────────────

    public async Task<ActionResult> RunFinalExamAsync(long enrollmentId, decimal grade, CancellationToken ct)
    {
        var steps = new List<object>();
        var enrollment = await db.Enrollments.AsNoTracking().FirstOrDefaultAsync(e => e.Id == enrollmentId, ct)
            ?? throw new InvalidOperationException("Enrollment no encontrado.");
        if (enrollment.Status != EnrollmentStatus.Regularized)
            throw new InvalidOperationException($"La mesa de examen requiere el enrollment en estado Regularized (actual: {enrollment.Status}). Cargá primero la cursada.");

        var spc = await ResolveStudyPlanCourseForEnrollment(enrollment, ct);
        var president = await EnsureTestTeacherAsync("A", ct);
        var vocal = await EnsureTestTeacherAsync("B", ct);
        var now = DateTime.UtcNow;

        var idem = $"exam-adhoc-{enrollment.CourseId}-{enrollmentId}";
        var table = await createExamTable.Handle(new CreateExamTableCommand(
            idem, enrollment.CourseId, now.Year, CallNumber: 1,
            ExamDateUtc: now.AddDays(7), RegistrationDeadlineUtc: now.AddDays(1),
            Location: "Aula Ad-hoc",
            Tribunal: [
                new ExamTribunalInput(president.Id, ExamTribunalRole.President),
                new ExamTribunalInput(vocal.Id, ExamTribunalRole.Vocal)
            ], president.UserId), ct);
        steps.Add(new { step = "Crear mesa de examen", status = "ok",
            detail = $"mesa {table.Id} (curso {enrollment.CourseId}, año {now.Year}, fecha {now.AddDays(7):yyyy-MM-dd}). Tribunal: docentes {president.Id}/{vocal.Id}.",
            data = new { examTableId = table.Id, presidentTeacherId = president.Id, vocalTeacherId = vocal.Id } });

        var reg = await registerForExam.Handle(new RegisterForExamCommand(table.Id, enrollmentId, president.UserId, IsAdmin: true), ct);
        await startExamGrading.Handle(new StartExamGradingCommand(table.Id, president.UserId), ct);
        await saveExamResults.Handle(new SaveExamResultsCommand(table.Id,
            [new ExamResultInput(reg.Id, ExamResultOutcome.Passed, grade, null)], president.UserId, IsAdmin: true), ct);
        await publishExamTable.Handle(new PublishExamTableCommand(table.Id, president.UserId), ct);

        var final = await db.Enrollments.AsNoTracking().FirstAsync(e => e.Id == enrollmentId, ct);
        steps.Add(new { step = "Registrar → calificar (Passed) → publicar", status = "ok",
            detail = $"Nota final {grade}. Estado del enrollment: {final.Status}.",
            data = new { status = final.Status.ToString(), finalGrade = final.FinalGrade } });

        return new ActionResult(true, $"Mesa publicada. Estado: {final.Status}.", steps);
    }

    // ── 5. Estado académico + elegibilidad ───────────────────────────────────────────────────

    public async Task<object> GetAcademicStateAsync(long studentId, int? careerId, CancellationToken ct)
    {
        var progress = await academicProgress.Handle(new GetStudentAcademicProgressQuery(studentId, careerId), ct);
        var eligible = await eligibleCourses.Handle(new GetEligibleCoursesForStudentQuery(studentId, careerId), ct);
        return new { progress, eligible };
    }

    // ── Atajo A: crear alumno de 1° año completo (con notas) ──────────────────────────────────

    /// <summary>
    /// Crea un alumno y deja TODO 1° año cursado/calificado: inscribe todas las materias de year==1
    /// del plan de la carrera y, por cada una, corre la cadena hasta condición final segun la nota
    /// (>=6 -> cursada Regularized + mesa Passed -> Approved; &lt;6 -> solo cursada -> Failed).
    /// Reusa CreateStudentAsync/EnrollMany/RunGradebook/RunFinalExam (mismas piezas que la línea de tiempo).
    /// </summary>
    public async Task<ActionResult> SetupFirstYearStudentAsync(
        string name, string lastName, string email, string password, string? dni, int careerId,
        IReadOnlyDictionary<int, decimal> gradesBySpcId, CancellationToken ct)
    {
        var steps = new List<object>();
        var effectiveDni = string.IsNullOrWhiteSpace(dni) ? $"9{DateTime.UtcNow.Ticks % 100000000:D8}"[..8] : dni.Trim();

        var created = (dynamic)await CreateStudentAsync(name, lastName, email, password, effectiveDni, careerId, ct);
        long studentId = created.studentId;
        steps.Add(new { step = "Crear alumno", status = "ok", detail = $"Alumno #{studentId} creado en carrera {careerId}.", data = new { studentId } });

        var plan = (await studyPlanRepository.GetByCareerIdAsync(careerId, ct)).FirstOrDefault()
            ?? throw new InvalidOperationException("La carrera no tiene plan de estudios.");
        var firstYear = (await studyPlanCourseRepository.GetByStudyPlanIdAsync(plan.Id, ct))
            .Where(spc => spc.YearNumber == 1).ToList();
        if (firstYear.Count == 0) throw new InvalidOperationException("El plan no tiene materias de 1° año.");

        // Algunas materias de 1° año tienen correlativas de OTRAS materias de 1° año (ej. C8<-C5).
        // Por eso no se puede inscribir todo junto: se procesa en OLEADAS — inscribir las materias
        // de 1° año que hoy son elegibles, calificarlas hasta condición final, y repetir hasta que
        // no queden materias de 1° año elegibles pendientes.
        var summary = new List<object>();
        var doneCourseIds = new HashSet<int>();
        var firstYearCourseIds = firstYear.Select(f => f.CourseId).ToHashSet();
        for (var wave = 1; wave <= firstYear.Count + 1; wave++)
        {
            if (doneCourseIds.Count >= firstYear.Count) break;
            var eligibleList = await eligibleCourses.Handle(new GetEligibleCoursesForStudentQuery(studentId, careerId), ct);
            var eligibleNow = eligibleList
                .Where(e => e.EligibilityStatus == "Eligible"
                    && firstYearCourseIds.Contains((int)e.CourseId)
                    && !doneCourseIds.Contains((int)e.CourseId))
                .Select(e => (int)e.StudyPlanCourseId)
                .ToList();
            if (eligibleNow.Count == 0) break; // no hay mas para avanzar (o quedaron bloqueadas)

            // Cada oleada usa su PROPIO período de inscripción: el handler rechaza inscribir 2 veces
            // al mismo alumno en el mismo período, así que no se puede reusar el de la oleada anterior.
            var wavePeriod = await enrollmentPeriodRepository.CreateAsync(new EnrollmentPeriod
            {
                CareerId = careerId, StudyPlanId = plan.Id, AcademicYear = 1, Semester = 1,
                QuotasMorning = 100, QuotasAfternoon = 100, QuotasEvening = 100, IsActive = true, StartDate = DateTime.UtcNow
            }, ct);
            await createEnrollment.Handle(new CreateEnrollmentCommand(studentId, wavePeriod.Id, "Mañana", eligibleNow), ct);
            foreach (var spcId in eligibleNow)
            {
                var spc = firstYear.First(f => f.Id == spcId);
                var score = gradesBySpcId.TryGetValue(spcId, out var s) ? s : 8m;
                var enrollment = await db.Enrollments.AsNoTracking()
                    .Where(x => x.StudentId == studentId && x.CourseId == spc.CourseId && x.AcademicYear == 1)
                    .OrderByDescending(x => x.Id).FirstAsync(ct);
                await RunGradebookAsync(enrollment.Id, score, teacherId: null, ct);
                if (score >= 6m) await RunFinalExamAsync(enrollment.Id, score, ct); // Regularized -> Approved
                var final = await db.Enrollments.AsNoTracking().FirstAsync(x => x.Id == enrollment.Id, ct);
                summary.Add(new { wave, code = spc.Course.Code, name = spc.Course.Name, score, status = final.Status.ToString() });
                doneCourseIds.Add(spc.CourseId);
            }
        }
        // Materias de 1° año que quedaron sin poder cursarse (correlativa interna no aprobada por nota baja)
        var notDone = firstYear.Where(f => !doneCourseIds.Contains(f.CourseId))
            .Select(f => new { code = f.Course.Code, reason = "correlativa de 1° año no aprobada (nota insuficiente en su prerequisito)" }).ToList();

        steps.Add(new { step = "Cursada 1° año (por oleadas segun correlativas)", status = notDone.Count == 0 ? "ok" : "warn",
            detail = $"{summary.Count} materia(s) procesadas; {notDone.Count} sin poder cursarse.",
            data = new { procesadas = summary, sinCursar = notDone } });

        return new ActionResult(true, $"Alumno #{studentId}: {summary.Count}/{firstYear.Count} materias de 1° año con condición final.", steps);
    }

    // ── Atajo B: crear profesor con sus materias ─────────────────────────────────────────────

    /// <summary>
    /// Crea un profesor (User Profesor + Teacher) y le asigna cada materia elegida, creando/reusando
    /// la Commission + TeachingPosition CON comisión (mismo criterio que el fix de la Parte 3: nunca
    /// un cargo sin comisión).
    /// </summary>
    public async Task<ActionResult> SetupTeacherWithCoursesAsync(
        string name, string lastName, string email, string password, string? dni, int careerId,
        IReadOnlyList<int> courseIds, int academicYear, int semester, CancellationToken ct)
    {
        var steps = new List<object>();
        var effectiveDni = string.IsNullOrWhiteSpace(dni) ? $"8{DateTime.UtcNow.Ticks % 100000000:D8}"[..8] : dni.Trim();

        var user = await userRepository.FindByEmailAsync(email, ct)
            ?? await userRepository.CreateAsync(email, name, lastName, password, effectiveDni, UserRole.Profesor, ct);
        var teacher = await teacherRepository.FindByUserIdAsync(user.Id, ct)
            ?? await teacherRepository.CreateAsync(new Teacher { UserId = user.Id, EmployeeNumber = $"DOC-{user.Id:D5}", HireDate = DateTime.UtcNow.Date, IsActive = true }, ct);
        steps.Add(new { step = "Crear profesor", status = "ok", detail = $"Profesor #{teacher.Id} (user {user.Id}).", data = new { teacherId = teacher.Id } });

        var year = academicYear > 0 ? academicYear : DateTime.UtcNow.Year;
        var sem = semester is 1 or 2 ? semester : 1;
        var assigned = new List<object>();
        foreach (var courseId in courseIds.Distinct())
        {
            var course = await courseRepository.FindByIdAsync(courseId, ct);
            if (course is null) { assigned.Add(new { courseId, status = "fail", detail = "Materia no encontrada." }); continue; }
            var spc = (await studyPlanCourseRepository.GetByStudyPlanIdAsync(
                (await studyPlanRepository.GetByCareerIdAsync(careerId, ct)).First().Id, ct))
                .FirstOrDefault(x => x.CourseId == courseId);
            var yearNumber = spc?.YearNumber ?? 1;
            var position = await ResolveOrCreateTeachingPositionForTeacherAsync(courseId, course.Code, careerId, year, sem, yearNumber, teacher, ct);
            try
            {
                var dto = await assignTeacher.Handle(new AcademiaDigital.Application.UseCases.Teachers.AssignTeacherCommand(
                    teacher.Id, position.PositionId, DateOnly.FromDateTime(DateTime.UtcNow), "Alta rápida dev-tools", teacher.UserId), ct);
                assigned.Add(new { courseId, code = course.Code, status = "ok", teachingPositionId = position.PositionId, commissionCode = position.CommissionCode, commissionReused = position.CommissionReused });
            }
            catch (Exception ex) { assigned.Add(new { courseId, code = course.Code, status = "warn", detail = ex.Message, teachingPositionId = position.PositionId, commissionCode = position.CommissionCode }); }
        }
        steps.Add(new { step = "Asignar materias", status = "ok", detail = $"{assigned.Count} materia(s) procesada(s).", data = assigned });

        return new ActionResult(true, $"Profesor #{teacher.Id} con {courseIds.Distinct().Count()} materia(s).", steps);
    }

    /// <summary>Crea/reusa Commission + TeachingPosition CON comisión para asignar un profesor (atajo B).</summary>
    private async Task<(int PositionId, string CommissionCode, bool CommissionReused)> ResolveOrCreateTeachingPositionForTeacherAsync(
        int courseId, string courseCode, int careerId, int academicYear, int semester, int yearNumber, Teacher teacher, CancellationToken ct)
    {
        var commissionCode = $"COM-DEV-{courseId}-{academicYear}";
        var commission = await db.Set<Commission>().FirstOrDefaultAsync(c => c.Code == commissionCode, ct);
        var reused = commission is not null;
        if (commission is null)
        {
            commission = new Commission { CareerId = careerId, Code = commissionCode, Name = $"Comisión dev {courseCode} {academicYear}", AcademicYear = academicYear, YearNumber = yearNumber, IsActive = true };
            db.Add(commission); await db.SaveChangesAsync(ct);
        }
        var position = await db.Set<TeachingPosition>()
            .FirstOrDefaultAsync(p => p.CourseId == courseId && p.CommissionId == commission.Id && p.IsActive && p.IsVacant, ct);
        if (position is null)
        {
            var now = DateTime.UtcNow;
            position = new TeachingPosition { CourseId = courseId, CommissionId = commission.Id, AcademicYear = academicYear, Semester = semester, PositionType = PositionType.Titular, MaxStudents = 100, IsVacant = true, IsActive = true, CreatedAt = now, UpdatedAt = now };
            db.Add(position); await db.SaveChangesAsync(ct);
        }
        return (position.Id, commission.Code, reused);
    }

    // ── Helpers (auto-crear/reusar Commission + TeachingPosition, con reporte) ────────────────

    private async Task<EnrollmentPeriod> ResolveOrCreatePeriodAsync(int careerId, int studyPlanId, int academicYear, int semester, List<object> steps, CancellationToken ct)
    {
        var existing = (await enrollmentPeriodRepository.GetAllAsync(ct))
            .FirstOrDefault(p => p.CareerId == careerId && p.StudyPlanId == studyPlanId
                && p.AcademicYear == academicYear && p.Semester == semester);
        if (existing is not null)
        {
            steps.Add(new { step = "Período de inscripción", status = "ok", detail = $"Reusado período existente id {existing.Id} ({academicYear}/{semester}).", data = new { periodId = existing.Id, reused = true } });
            return existing;
        }
        var created = await enrollmentPeriodRepository.CreateAsync(new EnrollmentPeriod
        {
            CareerId = careerId, StudyPlanId = studyPlanId, AcademicYear = academicYear, Semester = semester,
            QuotasMorning = 100, QuotasAfternoon = 100, QuotasEvening = 100, IsActive = true, StartDate = DateTime.UtcNow
        }, ct);
        steps.Add(new { step = "Período de inscripción", status = "ok", detail = $"Auto-creado período id {created.Id} ({academicYear}/{semester}, activo).", data = new { periodId = created.Id, reused = false } });
        return created;
    }

    /// <summary>Auto-crea o reusa Commission + TeachingPosition para la materia/año/cuatri, y lo reporta.</summary>
    private async Task<TeachingPosition> ResolveOrCreateTeachingPositionAsync(
        StudyPlanCourse spc, int courseId, int academicYear, int semester, long studentId, Teacher teacher, List<object> steps, CancellationToken ct)
    {
        // Per-student offering: cada alumno tiene su propia comisión/cargo para que el gradebook
        // (único por course-offering) no colisione al correr el atajo para más de un alumno.
        var commissionCode = $"COM-ADHOC-{courseId}-{academicYear}-S{studentId}";
        var commission = await db.Set<Commission>().FirstOrDefaultAsync(c => c.Code == commissionCode, ct);
        var commissionReused = commission is not null;
        if (commission is null)
        {
            commission = new Commission
            {
                CareerId = spc.CareerIdOrFallback(),
                Code = commissionCode,
                Name = $"Comisión ad-hoc {spc.Course.Code}",
                AcademicYear = academicYear,
                YearNumber = spc.YearNumber,
                IsActive = true
            };
            db.Add(commission);
            await db.SaveChangesAsync(ct);
        }

        var position = await db.Set<TeachingPosition>()
            .FirstOrDefaultAsync(p => p.CourseId == courseId && p.CommissionId == commission.Id, ct);
        var positionReused = position is not null;
        if (position is null)
        {
            var now = DateTime.UtcNow;
            position = new TeachingPosition
            {
                CourseId = courseId, CommissionId = commission.Id, AcademicYear = academicYear, Semester = semester,
                PositionType = PositionType.Titular, MaxStudents = 100, IsVacant = false, IsActive = true,
                TeacherId = teacher.Id, CreatedAt = now, UpdatedAt = now
            };
            db.Add(position);
            await db.SaveChangesAsync(ct);
        }

        steps.Add(new
        {
            step = "Comisión + cargo docente (auto)",
            status = "ok",
            detail = $"Comisión '{commission.Code}' ({(commissionReused ? "reusada" : "auto-creada")}), " +
                     $"cargo docente id {position.Id} ({(positionReused ? "reusado" : "auto-creado")}, docente {teacher.Id}).",
            data = new { commissionId = commission.Id, commissionCode = commission.Code, commissionReused,
                         teachingPositionId = position.Id, teachingPositionReused = positionReused, teacherId = teacher.Id }
        });
        return position;
    }

    private async Task<Teacher> EnsureTestTeacherAsync(string suffix, CancellationToken ct)
    {
        var email = $"adhoc.docente.{suffix.ToLowerInvariant()}@test.local";
        var user = await userRepository.FindByEmailAsync(email, ct)
            ?? await userRepository.CreateAsync(email, $"DocenteAdhoc{suffix}", "Test", "Docente123!",
                $"98{suffix[0]}00000"[..8], UserRole.Profesor, ct);
        return await teacherRepository.FindByUserIdAsync(user.Id, ct)
            ?? await teacherRepository.CreateAsync(new Teacher
            {
                UserId = user.Id, EmployeeNumber = $"DOC-ADHOC-{suffix}", HireDate = DateTime.UtcNow.Date, IsActive = true
            }, ct);
    }

    private async Task<StudyPlanCourse> ResolveStudyPlanCourseForEnrollment(Enrollment enrollment, CancellationToken ct)
    {
        if (enrollment.StudyPlanCourseId is null)
            throw new InvalidOperationException("El enrollment no tiene StudyPlanCourse asociado.");
        var spc = await studyPlanCourseRepository.GetByIdAsync(enrollment.StudyPlanCourseId.Value, ct)
            ?? throw new InvalidOperationException("No se encontró el StudyPlanCourse del enrollment.");
        return spc;
    }

    private async Task<long> FirstEvaluationId(long gradebookId, CancellationToken ct)
        => await db.Set<GradebookEvaluation>().Where(e => e.GradebookId == gradebookId)
            .OrderBy(e => e.DisplayOrder).Select(e => e.Id).FirstAsync(ct);
}

public sealed record ActionResult(bool Success, string Summary, IReadOnlyList<object> Steps);

internal static class StudyPlanCourseExtensions
{
    /// <summary>CareerId de la carrera del plan; si StudyPlan no vino incluido, cae al del Course.</summary>
    public static int CareerIdOrFallback(this StudyPlanCourse spc)
        => spc.StudyPlan?.CareerId ?? spc.Course.CareerId;
}
