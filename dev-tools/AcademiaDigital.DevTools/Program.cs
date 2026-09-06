using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Application.UseCases.StudyPlanImport;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;
using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Services;
using AcademiaDigital.DevTools;
using AcademiaDigital.Infrastructure;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Salvaguarda anti-producción (falla el arranque si no se cumple) ──────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
DevToolsSafety.EnsureSafeOrThrow(connectionString);

// ── Infraestructura real reutilizada (AppDbContext + repos + IPasswordHasher +
//    IUnitOfWork). Registra TODO lo del backend real. ──────────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// Domain services que el backend real registra en su Program.cs (AddInfrastructure NO los registra)
builder.Services.AddScoped<PrerequisiteCycleValidator>();
builder.Services.AddScoped<TeachingAssignmentPolicy>();
builder.Services.AddSingleton(TimeProvider.System);

// Use-cases / handlers reutilizados (mismos que usa la API real)
builder.Services.AddScoped<RegisterUseCase>();
builder.Services.AddScoped<StudyPlanCsvValidator>();
builder.Services.AddScoped<ImportStudyPlanFromCsvCommandHandler>();
builder.Services.AddScoped<AssignTeacherCommandHandler>();

// Servicio propio de la herramienta (reset+reseed + listados)
builder.Services.AddScoped<DevToolsService>();

// ── Escenario "correlativa bloqueante" ───────────────────────────────────────
// Policies + servicios de dominio que el escenario necesita (los repos vienen de AddInfrastructure).
builder.Services.AddScoped<GradebookPolicy>();
builder.Services.AddScoped<ExamTablePolicy>();
builder.Services.AddScoped<CourseEligibilityService>();
builder.Services.AddScoped<EnrollmentEligibilityPolicy>();
builder.Services.AddScoped<EnrollmentCapacityPolicy>();
// Handlers reales reutilizados por el escenario (cursada + mesa de examen + inscripción).
builder.Services.AddScoped<CreateEnrollmentCommandHandler>();
builder.Services.AddScoped<CreateGradebookCommandHandler>();
builder.Services.AddScoped<SaveGradeEntriesCommandHandler>();
builder.Services.AddScoped<SubmitGradebookCommandHandler>();
builder.Services.AddScoped<ApproveGradebookCommandHandler>();
builder.Services.AddScoped<PublishGradebookCommandHandler>();
builder.Services.AddScoped<CloseGradebookCommandHandler>();
builder.Services.AddScoped<CreateExamTableCommandHandler>();
builder.Services.AddScoped<RegisterForExamCommandHandler>();
builder.Services.AddScoped<StartExamGradingCommandHandler>();
builder.Services.AddScoped<SaveExamResultsCommandHandler>();
builder.Services.AddScoped<PublishExamTableCommandHandler>();
builder.Services.AddScoped<CorrelativaScenarioService>();

// ── Acciones académicas ad-hoc (sección nueva, aparte del escenario fijo) ────
builder.Services.AddScoped<AcademicProgressCalculator>();
builder.Services.AddScoped<GetStudentAcademicProgressQueryHandler>();
builder.Services.AddScoped<GetEligibleCoursesForStudentQueryHandler>();
builder.Services.AddScoped<AcademicActionsService>();

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseDefaultFiles();  // sirve wwwroot/index.html en "/"
app.UseStaticFiles();
app.UseCors();

// ── Info / salud ─────────────────────────────────────────────────────────────
app.MapGet("/api/info", (IConfiguration cfg) => Results.Ok(new
{
    tool = "AcademiaDigital DevTools",
    target = DevToolsSafety.DescribeTarget(cfg.GetConnectionString("DefaultConnection")),
    warning = "Herramienta de desarrollo/testing. Operaciones destructivas."
}));

// ── (5) Listado general ──────────────────────────────────────────────────────
app.MapGet("/api/overview", async (DevToolsService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetOverviewAsync(ct)));

app.MapGet("/api/careers", async (DevToolsService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetCareersAsync(ct)));

app.MapGet("/api/careers/{careerId:int}/courses", async (int careerId, DevToolsService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetCoursesByCareerAsync(careerId, ct)));

app.MapGet("/api/teachers", async (DevToolsService svc, CancellationToken ct) =>
    Results.Ok(await svc.GetTeachersAsync(ct)));

// ── (1) Reset completo + reseed ──────────────────────────────────────────────
// Requiere confirm=true explícito en el body (además del modal en la UI).
app.MapPost("/api/reset", async (ResetRequest req, DevToolsService svc, CancellationToken ct) =>
{
    if (req is null || !req.Confirm)
        return Results.BadRequest(new { error = "Falta confirmación explícita (confirm=true)." });
    try
    {
        var report = await svc.ResetAndReseedAsync(ct);
        return Results.Ok(report);
    }
    catch (Exception ex)
    {
        return Results.Problem(title: "Reset falló", detail: ex.Message, statusCode: 500);
    }
});

// ── Escenario end-to-end: "correlativa bloqueante" ───────────────────────────
// Ejercita cursada + mesa de examen reales sobre DS2023 y verifica que la inscripción a
// Programación II se bloquee por la correlativa Programación I (sin aprobar). Devuelve
// { result: PASS|FAIL, summary, steps[] } con reporte paso a paso para diagnóstico.
app.MapPost("/api/scenario/correlativa-bloqueante", async (CorrelativaScenarioService scenario, CancellationToken ct) =>
{
    var report = await scenario.RunAsync(ct);
    // 200 siempre: el resultado del escenario (PASS/FAIL) va en el body; un FAIL no es un error HTTP.
    return Results.Ok(report);
});

// ── (2) Crear Carrera + importar Plan de Estudios por CSV ────────────────────
// Reusa ImportStudyPlanFromCsvCommandHandler. Si viene careerId usa esa carrera;
// si vienen datos de carrera nueva (code/name), la crea antes con ICareerRepository.
app.MapPost("/api/careers/import-plan", async (
    HttpRequest http,
    ICareerRepository careerRepo,
    ImportStudyPlanFromCsvCommandHandler importHandler,
    CancellationToken ct) =>
{
    if (!http.HasFormContentType)
        return Results.BadRequest(new { error = "Se espera multipart/form-data con el CSV." });

    var form = await http.ReadFormAsync(ct);
    var file = form.Files["file"];
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "Falta el archivo CSV (campo 'file')." });

    int careerId;
    var existingCareerId = form["careerId"].ToString();
    if (!string.IsNullOrWhiteSpace(existingCareerId) && int.TryParse(existingCareerId, out var parsed))
    {
        careerId = parsed;
    }
    else
    {
        // Crear carrera nueva a partir de los campos del form
        var code = form["careerCode"].ToString().Trim();
        var name = form["careerName"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return Results.BadRequest(new { error = "Para crear una carrera nueva se requieren 'careerCode' y 'careerName' (o pasar 'careerId' de una existente)." });

        var existing = await careerRepo.FindByCodeAsync(code, ct);
        if (existing is not null)
            return Results.BadRequest(new { error = $"Ya existe una carrera con code '{code}' (id {existing.Id}). Usá careerId={existing.Id} para importar el plan sobre ella." });

        _ = int.TryParse(form["durationYears"].ToString(), out var durationYears);
        var created = await careerRepo.CreateAsync(Career.Create(
            name,
            code,
            form["careerDescription"].ToString(),
            durationYears > 0 ? durationYears : 1), ct);
        careerId = created.Id;
    }

    var planCode = form["planCode"].ToString();
    var planName = form["planName"].ToString();
    if (string.IsNullOrWhiteSpace(planCode)) planCode = $"PLAN-{careerId}";
    if (string.IsNullOrWhiteSpace(planName)) planName = "Plan de estudios";

    await using var stream = file.OpenReadStream();
    var command = new ImportStudyPlanFromCsvCommand(
        careerId, planCode.Trim(), planName.Trim(), 1, null, null, stream);

    try
    {
        var result = await importHandler.Handle(command, ct);
        if (!result.Success)
            return Results.BadRequest(new { success = false, careerId, errors = result.Errors });
        return Results.Ok(new
        {
            success = true,
            careerId,
            studyPlanId = result.StudyPlanId,
            coursesCreated = result.CoursesCreated,
            prerequisitesCreated = result.PrerequisitesCreated
        });
    }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (Exception ex) { return Results.Problem(title: "Import falló", detail: ex.Message, statusCode: 500); }
});

// ── (3) Alta de usuarios (3 roles) ───────────────────────────────────────────
app.MapPost("/api/users", async (
    CreateUserRequest req,
    IUserRepository userRepo,
    ITeacherRepository teacherRepo,
    RegisterUseCase registerUseCase,
    CancellationToken ct) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password)
        || string.IsNullOrWhiteSpace(req.Dni) || string.IsNullOrWhiteSpace(req.Name))
        return Results.BadRequest(new { error = "Faltan campos obligatorios (name, email, password, dni)." });

    if (!Enum.IsDefined(typeof(UserRole), req.Role))
        return Results.BadRequest(new { error = "Rol inválido. Alumno=1, Profesor=2, Admin=3." });

    var role = (UserRole)req.Role;
    var lastName = req.LastName ?? string.Empty;

    try
    {
        if (role == UserRole.Alumno)
        {
            // Alumno: reusa RegisterUseCase (crea User+Student+StudentCareer). Requiere careerId.
            if (req.CareerId is null or <= 0)
                return Results.BadRequest(new { error = "Para un Alumno se requiere 'careerId' (carrera existente)." });

            var result = await registerUseCase.ExecuteAsync(
                req.Email, req.Name, lastName, req.Password, req.Dni, req.CareerId.Value, ct);
            return Results.Ok(new { success = result.Success, userId = result.UserId, role = role.ToString() });
        }

        // Profesor / Admin: reusa IUserRepository.CreateAsync (hashea BCrypt internamente).
        var user = await userRepo.CreateAsync(req.Email, req.Name, lastName, req.Password, req.Dni, role, ct);

        // Si es Profesor, además creamos su perfil Teacher (necesario para poder asignarle
        // materias: TeacherAssignment referencia Teacher.Id, no User.Id). Reusa ITeacherRepository.
        if (role == UserRole.Profesor)
        {
            var now = DateTime.UtcNow;
            var teacher = await teacherRepo.CreateAsync(new Teacher
            {
                UserId = user.Id,
                EmployeeNumber = $"DOC-{user.Id:D5}",
                HireDate = now.Date,
                IsActive = true
            }, ct);
            return Results.Ok(new { success = true, userId = user.Id, teacherId = teacher.Id, role = role.ToString() });
        }

        return Results.Ok(new { success = true, userId = user.Id, role = role.ToString() });
    }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ── (4) Asignar materia (curso) a un profesor ────────────────────────────────
// Crea/elige una TeachingPosition para el curso y luego el TeacherAssignment.
app.MapPost("/api/teacher-assignments", async (
    AssignRequest req,
    ITeacherRepository teacherRepo,
    ITeachingPositionRepository positionRepo,
    AssignTeacherCommandHandler assignHandler,
    ICourseRepository courseRepo,
    AppDbContext db,
    CancellationToken ct) =>
{
    if (req is null || req.TeacherId <= 0 || req.CourseId <= 0)
        return Results.BadRequest(new { error = "Se requieren 'teacherId' y 'courseId'." });

    var teacher = await teacherRepo.FindByIdAsync(req.TeacherId, ct);
    if (teacher is null) return Results.NotFound(new { error = "Profesor no encontrado." });

    var course = await courseRepo.FindByIdAsync(req.CourseId, ct);
    if (course is null) return Results.NotFound(new { error = "Materia (course) no encontrada." });

    var academicYear = req.AcademicYear > 0 ? req.AcademicYear : DateTime.UtcNow.Year;
    var semester = req.Semester is 1 or 2 ? req.Semester : 1;

    try
    {
        // Un cargo docente SIN comisión rompe después 'Crear planilla' (el gradebook exige
        // CommissionId). Por eso acá SIEMPRE se garantiza una comisión: se auto-crea o reusa una
        // para la materia/año, y se linkea al cargo. Solo se reusa un cargo que YA tenga comisión.
        var commissionCode = $"COM-DEV-{req.CourseId}-{academicYear}";
        var commission = await db.Set<Commission>().FirstOrDefaultAsync(c => c.Code == commissionCode, ct);
        var commissionReused = commission is not null;
        if (commission is null)
        {
            commission = new Commission
            {
                CareerId = course.CareerId,
                Code = commissionCode,
                Name = $"Comisión dev {course.Code} {academicYear}",
                AcademicYear = academicYear,
                YearNumber = 1,
                IsActive = true
            };
            db.Add(commission);
            await db.SaveChangesAsync(ct);
        }

        // Reusar una TeachingPosition vacante del curso CON comisión para ese período, o crear una nueva CON comisión.
        var positions = await positionRepo.GetByCourseAsync(req.CourseId, ct);
        var position = positions.FirstOrDefault(p =>
            p.IsActive && p.IsVacant && p.CommissionId == commission.Id
            && p.AcademicYear == academicYear && p.Semester == semester);

        if (position is null)
        {
            var now = DateTime.UtcNow;
            position = await positionRepo.CreateAsync(new TeachingPosition
            {
                CourseId = req.CourseId,
                CommissionId = commission.Id,
                AcademicYear = academicYear,
                Semester = semester,
                PositionType = PositionType.Titular,
                MaxStudents = req.MaxStudents > 0 ? req.MaxStudents : 100,
                IsVacant = true,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }, ct);
        }

        var command = new AssignTeacherCommand(
            req.TeacherId,
            position.Id,
            DateOnly.FromDateTime(DateTime.UtcNow),
            req.Reason,
            ActorUserId: teacher.UserId); // actor = el propio profesor (herramienta de dev, sin auth)

        var dto = await assignHandler.Handle(command, ct);
        return Results.Ok(new
        {
            success = true,
            assignment = dto,
            teachingPositionId = position.Id,
            commission = new { commission.Id, commission.Code, reused = commissionReused }
        });
    }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// ═══ Acciones académicas ad-hoc (sección nueva, aparte del escenario fijo) ═══

// -- Endpoints de apoyo para poblar los selectores de la UI --
app.MapGet("/api/actions/careers/{careerId:int}/study-plans", async (int careerId, IStudyPlanRepository plans, CancellationToken ct) =>
    Results.Ok((await plans.GetByCareerIdAsync(careerId, ct))
        .Select(p => new { p.Id, p.Code, p.Name, status = p.Status.ToString() })));

// Materias de un plan (para elegir en qué inscribir / calificar).
app.MapGet("/api/actions/study-plans/{studyPlanId:int}/courses", async (int studyPlanId, IStudyPlanCourseRepository spcRepo, CancellationToken ct) =>
    Results.Ok((await spcRepo.GetByStudyPlanIdAsync(studyPlanId, ct))
        .Select(spc => new { studyPlanCourseId = spc.Id, courseId = spc.CourseId, code = spc.Course.Code, name = spc.Course.Name, year = spc.YearNumber, semester = spc.Semester })));

// (1) Alumnos: listar (opcional por carrera) / crear
app.MapGet("/api/actions/students", async (int? careerId, AcademicActionsService svc, CancellationToken ct) =>
    Results.Ok(await svc.ListStudentsAsync(careerId, ct)));

app.MapPost("/api/actions/students", async (CreateUserRequest req, AcademicActionsService svc, CancellationToken ct) =>
{
    if (req is null || req.CareerId is null or <= 0)
        return Results.BadRequest(new { error = "Se requieren name, email, password, dni y careerId (el alumno pertenece a una carrera)." });
    try { return Results.Ok(await svc.CreateStudentAsync(req.Name, req.LastName ?? "", req.Email, req.Password, req.Dni, req.CareerId.Value, ct)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// (2) Inscribir en materias (año/cuatri libres)
app.MapPost("/api/actions/enroll", async (EnrollActionRequest req, AcademicActionsService svc, CancellationToken ct) =>
{
    if (req is null || req.StudentId <= 0 || req.StudyPlanCourseIds is null || req.StudyPlanCourseIds.Count == 0)
        return Results.BadRequest(new { error = "Se requieren studentId y studyPlanCourseIds[]." });
    try
    {
        var year = req.AcademicYear > 0 ? req.AcademicYear : 1;
        var sem = req.Semester is 1 or 2 ? req.Semester : 1;
        return Results.Ok(await svc.EnrollManyAsync(req.StudentId, req.StudyPlanCourseIds, year, sem, ct));
    }
    catch (Exception ex) { return Results.Ok(new ActionResult(false, ex.Message, [new { step = "Inscripción", status = "fail", detail = ex.Message }])); }
});

// (3) Cargar notas (cadena Gradebook completa) sobre un enrollment
app.MapPost("/api/actions/gradebook", async (GradebookActionRequest req, AcademicActionsService svc, CancellationToken ct) =>
{
    if (req is null || req.EnrollmentId <= 0)
        return Results.BadRequest(new { error = "Se requiere enrollmentId (y score 0-10)." });
    try { return Results.Ok(await svc.RunGradebookAsync(req.EnrollmentId, req.Score, req.TeacherId, ct)); }
    catch (Exception ex) { return Results.Ok(new ActionResult(false, ex.Message, [new { step = "Gradebook", status = "fail", detail = ex.Message }])); }
});

// (4) Mesa de examen (Regularized -> Approved)
app.MapPost("/api/actions/exam", async (ExamActionRequest req, AcademicActionsService svc, CancellationToken ct) =>
{
    if (req is null || req.EnrollmentId <= 0)
        return Results.BadRequest(new { error = "Se requiere enrollmentId (y grade 6-10)." });
    try { return Results.Ok(await svc.RunFinalExamAsync(req.EnrollmentId, req.Grade, ct)); }
    catch (Exception ex) { return Results.Ok(new ActionResult(false, ex.Message, [new { step = "Mesa de examen", status = "fail", detail = ex.Message }])); }
});

// (5) Estado académico + elegibilidad del alumno
app.MapGet("/api/actions/students/{studentId:long}/academic-state", async (long studentId, int? careerId, AcademicActionsService svc, CancellationToken ct) =>
{
    try { return Results.Ok(await svc.GetAcademicStateAsync(studentId, careerId, ct)); }
    catch (Exception ex) { return Results.BadRequest(new { error = ex.Message }); }
});

// Atajo A: crear alumno de 1° año completo (con notas)
app.MapPost("/api/actions/setup-first-year-student", async (SetupFirstYearRequest req, AcademicActionsService svc, CancellationToken ct) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Email) || req.CareerId <= 0)
        return Results.BadRequest(new { error = "Se requieren name, email, password y careerId." });
    try
    {
        var grades = (req.Grades ?? []).ToDictionary(g => g.StudyPlanCourseId, g => g.Score ?? 8m);
        return Results.Ok(await svc.SetupFirstYearStudentAsync(req.Name, req.LastName ?? "", req.Email, req.Password, req.Dni, req.CareerId, grades, ct));
    }
    catch (Exception ex) { return Results.Ok(new ActionResult(false, ex.Message, [new { step = "Alta 1° año", status = "fail", detail = ex.Message }])); }
});

// Atajo B: crear profesor con sus materias
app.MapPost("/api/actions/setup-teacher-with-courses", async (SetupTeacherRequest req, AcademicActionsService svc, CancellationToken ct) =>
{
    if (req is null || string.IsNullOrWhiteSpace(req.Email) || req.CareerId <= 0 || req.CourseIds is null || req.CourseIds.Count == 0)
        return Results.BadRequest(new { error = "Se requieren name, email, password, careerId y al menos una materia." });
    try
    {
        var year = req.AcademicYear > 0 ? req.AcademicYear : DateTime.UtcNow.Year;
        var sem = req.Semester is 1 or 2 ? req.Semester : 1;
        return Results.Ok(await svc.SetupTeacherWithCoursesAsync(req.Name, req.LastName ?? "", req.Email, req.Password, req.Dni, req.CareerId, req.CourseIds, year, sem, ct));
    }
    catch (Exception ex) { return Results.Ok(new ActionResult(false, ex.Message, [new { step = "Alta profesor", status = "fail", detail = ex.Message }])); }
});

app.Run();

// ── DTOs de request ──────────────────────────────────────────────────────────
internal sealed record ResetRequest(bool Confirm);
internal sealed record CreateUserRequest(string Name, string? LastName, string Email, string Password, string Dni, int Role, int? CareerId);
internal sealed record EnrollActionRequest(long StudentId, IReadOnlyList<int> StudyPlanCourseIds, int AcademicYear, int Semester);
internal sealed record GradebookActionRequest(long EnrollmentId, decimal Score, long? TeacherId);
internal sealed record ExamActionRequest(long EnrollmentId, decimal Grade);
internal sealed record CourseGradeInput(int StudyPlanCourseId, decimal? Score);
internal sealed record SetupFirstYearRequest(string Name, string? LastName, string Email, string Password, string? Dni, int CareerId, IReadOnlyList<CourseGradeInput>? Grades);
internal sealed record SetupTeacherRequest(string Name, string? LastName, string Email, string Password, string? Dni, int CareerId, IReadOnlyList<int> CourseIds, int AcademicYear, int Semester);
internal sealed record AssignRequest(long TeacherId, int CourseId, int AcademicYear, int Semester, int MaxStudents, string? Reason);
