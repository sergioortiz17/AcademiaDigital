using AcademiaDigital.API.Middleware;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Application.UseCases.Attendance;
using AcademiaDigital.Application.UseCases.Admissions;
using AcademiaDigital.Application.UseCases.Enrollments;
using AcademiaDigital.Application.UseCases.Grades;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.Certificates;
using AcademiaDigital.Application.UseCases.Careers;
using AcademiaDigital.Application.UseCases.Courses;
using AcademiaDigital.Application.UseCases.Prerequisites;
using AcademiaDigital.Application.UseCases.Students;
using AcademiaDigital.Application.UseCases.StudyPlanCourses;
using AcademiaDigital.Application.UseCases.StudyPlanDiff;
using AcademiaDigital.Application.UseCases.StudyPlanImport;
using AcademiaDigital.Application.UseCases.StudyPlans;
using AcademiaDigital.Application.UseCases.Teachers;
using AcademiaDigital.Application.UseCases.Tenants;
using AcademiaDigital.Application.UseCases.User;
using AcademiaDigital.Domain.Services;
using AcademiaDigital.Infrastructure;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using AcademiaDigital.API.Observability;
using AcademiaDigital.API.Security;
using Npgsql;
using AcademiaDigital.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Observabilidad: logs JSON, IP real del cliente (ver ObservabilityExtensions) ──
builder.AddObservability();

// ── Infrastructure (EF Core, repositories, JWT service) ─────────────────────
builder.Services.AddInfrastructure(builder.Configuration);

// ── Application use-cases ────────────────────────────────────────────────────
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RegisterUseCase>();
builder.Services.AddScoped<LogoutUseCase>();
builder.Services.AddScoped<GetProfileUseCase>();
builder.Services.AddScoped<UpdateUserUseCase>();
builder.Services.AddScoped<ChangePasswordUseCase>();
builder.Services.AddScoped<ForgotPasswordUseCase>();
builder.Services.AddScoped<ResetPasswordUseCase>();

// Admin
builder.Services.AddScoped<GetUsersUseCase>();
builder.Services.AddScoped<UpdateUserRoleUseCase>();
builder.Services.AddScoped<UpdateUserActiveStatusUseCase>();
builder.Services.AddScoped<DeleteUserUseCase>();
builder.Services.AddScoped<GetApplicantsUseCase>();
builder.Services.AddScoped<ApproveApplicantsUseCase>();

// Tenants (multitenant: alta de instituciones por el operador de la plataforma)
builder.Services.AddScoped<TenantPolicy>();
builder.Services.AddScoped<GetTenantsQueryHandler>();
builder.Services.AddScoped<CreateTenantCommandHandler>();

// Admissions
// Time-travel (dev): un único OverridableTimeProvider reemplaza TimeProvider.System, para que todos
// los handlers que inyectan TimeProvider usen el reloj simulable. El control se gatea por
// ALLOW_TIME_TRAVEL en TimeTravelController. Se registra la MISMA instancia bajo ambos tipos.
var timeProvider = new AcademiaDigital.Infrastructure.Time.OverridableTimeProvider();
builder.Services.AddSingleton(timeProvider);
builder.Services.AddSingleton<TimeProvider>(timeProvider);
builder.Services.AddScoped<AdmissionApplicationPolicy>();
builder.Services.AddScoped<AdmissionFormPolicy>();
builder.Services.AddScoped<AdmissionStatusTransitionPolicy>();
builder.Services.AddScoped<AdmissionCapacityPolicy>();
builder.Services.AddScoped<AdmissionTargetPolicy>();
builder.Services.AddScoped<AdmissionDocumentPolicy>();
builder.Services.AddScoped<AdmissionCapacityCoordinator>();
builder.Services.AddScoped<GetAdmissionFormQueryHandler>();
builder.Services.AddScoped<CreateAdmissionApplicationCommandHandler>();
builder.Services.AddScoped<GetAdmissionFormsQueryHandler>();
builder.Services.AddScoped<CreateAdmissionFormCommandHandler>();
builder.Services.AddScoped<SetAdmissionFormActiveCommandHandler>();
builder.Services.AddScoped<SetAdmissionFormCapacityCommandHandler>();
builder.Services.AddScoped<GetAdmissionApplicationsQueryHandler>();
builder.Services.AddScoped<GetAdmissionApplicationQueryHandler>();
builder.Services.AddScoped<ChangeAdmissionApplicationStatusCommandHandler>();
builder.Services.AddScoped<ProcessAdmissionExpirationsCommandHandler>();
builder.Services.AddScoped<GetAdmissionApplicationDocumentsQueryHandler>();
builder.Services.AddScoped<SubmitAdmissionApplicationDocumentCommandHandler>();
builder.Services.AddScoped<ReviewAdmissionApplicationDocumentCommandHandler>();
builder.Services.AddScoped<GetAdmissionAgreementQueryHandler>();
builder.Services.AddScoped<DownloadAdmissionAgreementQueryHandler>();
builder.Services.AddScoped<ProcessAdmissionOutboxCommandHandler>();
builder.Services.AddScoped<StudentRematriculationPolicy>();
builder.Services.AddScoped<CreateStudentRematriculationCommandHandler>();

// Certificates
builder.Services.AddScoped<GetCertificateRequestsUseCase>();
builder.Services.AddScoped<CreateCertificateRequestUseCase>();
builder.Services.AddScoped<GetAllCertificateRequestsUseCase>();
builder.Services.AddScoped<CertificatePolicy>();
builder.Services.AddScoped<ReviewCertificateRequestCommandHandler>();
builder.Services.AddScoped<IssueCertificateCommandHandler>();
builder.Services.AddScoped<GetCertificateHistoryQueryHandler>();
builder.Services.AddScoped<DownloadCertificateQueryHandler>();

// Finance module extracted to microservice (services/AcademiaDigital.Finance).
// CareerService is NOT part of Finance; it stays in the monolith.
builder.Services.AddScoped<CareerService>();

// Academic module
builder.Services.AddScoped<PrerequisiteCycleValidator>();
builder.Services.AddScoped<StudyPlanCsvValidator>();
builder.Services.AddScoped<CourseEligibilityService>();
builder.Services.AddScoped<EnrollmentEligibilityPolicy>();
builder.Services.AddScoped<EnrollmentCapacityPolicy>();
builder.Services.AddScoped<AcademicProgressCalculator>();
builder.Services.AddScoped<GetCareerCoursesQueryHandler>();
builder.Services.AddScoped<CreateCourseCommandHandler>();
builder.Services.AddScoped<UpdateCourseCommandHandler>();
builder.Services.AddScoped<DeleteCourseCommandHandler>();
builder.Services.AddScoped<GetCareerStudyPlansQueryHandler>();
builder.Services.AddScoped<CreateStudyPlanCommandHandler>();
builder.Services.AddScoped<UpdateStudyPlanCommandHandler>();
builder.Services.AddScoped<ActivateStudyPlanCommandHandler>();
builder.Services.AddScoped<GetCareerStudyPlanGroupedQueryHandler>();
builder.Services.AddScoped<ImportStudyPlanFromCsvCommandHandler>();
builder.Services.AddScoped<GetStudyPlanDiffQueryHandler>();
builder.Services.AddScoped<PreviewStudyPlanDiffCommandHandler>();
builder.Services.AddScoped<GetStudyPlanCoursesQueryHandler>();
builder.Services.AddScoped<AddCourseToStudyPlanCommandHandler>();
builder.Services.AddScoped<UpdateStudyPlanCourseCommandHandler>();
builder.Services.AddScoped<RemoveCourseFromStudyPlanCommandHandler>();
builder.Services.AddScoped<SetCourseApprovalRuleCommandHandler>();
builder.Services.AddScoped<GetCoursePrerequisitesQueryHandler>();
builder.Services.AddScoped<AddCoursePrerequisiteCommandHandler>();
builder.Services.AddScoped<RemoveCoursePrerequisiteCommandHandler>();
builder.Services.AddScoped<GetEligibleCoursesForStudentQueryHandler>();
builder.Services.AddScoped<GetStudentAcademicProgressQueryHandler>();
builder.Services.AddScoped<AssignStudentStudyPlanCommandHandler>();
builder.Services.AddScoped<GetStudentsQueryHandler>();
builder.Services.AddScoped<GetStudentByIdQueryHandler>();
builder.Services.AddScoped<CreateStudentCommandHandler>();

// Teachers
builder.Services.AddScoped<TeacherProfilePolicy>();
builder.Services.AddScoped<GetTeachersQueryHandler>();
builder.Services.AddScoped<GetTeacherByIdQueryHandler>();
builder.Services.AddScoped<CreateTeacherCommandHandler>();
builder.Services.AddScoped<UpdateTeacherCommandHandler>();
builder.Services.AddScoped<DeactivateTeacherCommandHandler>();
builder.Services.AddScoped<TeacherDocumentPolicy>();
builder.Services.AddScoped<GetTeacherDocumentsQueryHandler>();
builder.Services.AddScoped<SubmitTeacherDocumentCommandHandler>();
builder.Services.AddScoped<ReviewTeacherDocumentCommandHandler>();
builder.Services.AddScoped<TeachingAssignmentPolicy>();
builder.Services.AddScoped<GetCourseSectionsQueryHandler>();
builder.Services.AddScoped<GetCourseSectionByIdQueryHandler>();
builder.Services.AddScoped<CreateCourseSectionCommandHandler>();
builder.Services.AddScoped<UpdateCourseSectionCommandHandler>();
builder.Services.AddScoped<DeactivateCourseSectionCommandHandler>();
builder.Services.AddScoped<GetTeacherAssignmentsQueryHandler>();
builder.Services.AddScoped<GetMyTeacherAssignmentsQueryHandler>();
builder.Services.AddScoped<AssignTeacherCommandHandler>();
builder.Services.AddScoped<EndTeacherAssignmentCommandHandler>();

// Attendance
builder.Services.AddScoped<AttendancePolicy>();
builder.Services.AddScoped<GetAttendanceSessionsQueryHandler>();
builder.Services.AddScoped<GetAttendanceSessionQueryHandler>();
builder.Services.AddScoped<CreateAttendanceSessionCommandHandler>();
builder.Services.AddScoped<SaveAttendanceRecordsCommandHandler>();
builder.Services.AddScoped<CloseAttendanceSessionCommandHandler>();
builder.Services.AddScoped<ReopenAttendanceSessionCommandHandler>();
builder.Services.AddScoped<JustifyAttendanceRecordCommandHandler>();
builder.Services.AddScoped<GetStudentAttendanceSummaryQueryHandler>();
builder.Services.AddScoped<GetMyAttendanceSummaryQueryHandler>();
builder.Services.AddScoped<ExportAttendanceSessionQueryHandler>();

// Grades and exam tables
builder.Services.AddScoped<GradebookPolicy>();
builder.Services.AddScoped<ExamTablePolicy>();
builder.Services.AddScoped<GetGradebooksQueryHandler>();
builder.Services.AddScoped<GetGradebookQueryHandler>();
builder.Services.AddScoped<CreateGradebookCommandHandler>();
builder.Services.AddScoped<SaveGradeEntriesCommandHandler>();
builder.Services.AddScoped<SubmitGradebookCommandHandler>();
builder.Services.AddScoped<ApproveGradebookCommandHandler>();
builder.Services.AddScoped<PublishGradebookCommandHandler>();
builder.Services.AddScoped<CloseGradebookCommandHandler>();
builder.Services.AddScoped<ReopenGradebookCommandHandler>();
builder.Services.AddScoped<GetMyGradesQueryHandler>();
builder.Services.AddScoped<GetExamTablesQueryHandler>();
builder.Services.AddScoped<GetExamTableQueryHandler>();
builder.Services.AddScoped<CreateExamTableCommandHandler>();
builder.Services.AddScoped<RegisterForExamCommandHandler>();
builder.Services.AddScoped<StartExamGradingCommandHandler>();
builder.Services.AddScoped<SaveExamResultsCommandHandler>();
builder.Services.AddScoped<PublishExamTableCommandHandler>();
builder.Services.AddScoped<ReopenExamTableCommandHandler>();
builder.Services.AddScoped<GetMyExamTablesQueryHandler>();

// Enrollment periods
builder.Services.AddScoped<GetAllEnrollmentPeriodsQueryHandler>();
builder.Services.AddScoped<GetActiveEnrollmentPeriodQueryHandler>();
builder.Services.AddScoped<GetEnrolledStudentsQueryHandler>();
builder.Services.AddScoped<OpenEnrollmentPeriodCommandHandler>();
builder.Services.AddScoped<CloseEnrollmentPeriodCommandHandler>();
builder.Services.AddScoped<UpdatePeriodQuotasCommandHandler>();
builder.Services.AddScoped<RemoveStudentFromPeriodCommandHandler>();
builder.Services.AddScoped<GetMyEnrollmentsQueryHandler>();
builder.Services.AddScoped<ActivateEnrollmentPeriodCommandHandler>();
builder.Services.AddScoped<DeleteEnrollmentPeriodCommandHandler>();
builder.Services.AddScoped<GetPeriodReportQueryHandler>();
builder.Services.AddScoped<GetPeriodDivisionCoverageQueryHandler>();
builder.Services.AddScoped<EnrollmentPeriodFacade>();
builder.Services.AddScoped<EnrollmentPeriodAdminFacade>();
builder.Services.AddScoped<CreateEnrollmentCommandHandler>();
builder.Services.AddScoped<AdminApproveEnrollmentCommandHandler>();

// ── CORS ─────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

// Además de la lista fija, se aceptan los dominios registrados de cada institución: dar de alta
// una institución nueva no requiere tocar configuración ni reiniciar.
builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>().Configure<ITenantDirectory>((options, directory) =>
    options.AddDefaultPolicy(policy =>
        policy.SetIsOriginAllowed(origin =>
                allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)
                || (Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && directory.IsKnownHost(uri.Host.ToLowerInvariant())))
              .AllowAnyHeader()
              .AllowAnyMethod()));

// Public admission anti-abuse. Partition strictly by the connection IP; trusted
// reverse proxies must configure forwarded headers at the deployment boundary.
var admissionRateLimit = builder.Configuration.GetSection("AdmissionAntiAbuse:RateLimit");
var admissionRateLimitEnabled = admissionRateLimit.GetValue("Enabled", true);
var admissionPermitLimit = Math.Clamp(admissionRateLimit.GetValue("PermitLimit", 10), 1, 1000);
var admissionWindowSeconds = Math.Clamp(admissionRateLimit.GetValue("WindowSeconds", 60), 1, 3600);
var admissionQueueLimit = Math.Clamp(admissionRateLimit.GetValue("QueueLimit", 0), 0, 100);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        await context.HttpContext.Response.WriteAsJsonAsync(
            new { success = false, msg = "Too many admission attempts. Please retry later." },
            cancellationToken);
    };
    options.AddPolicy("PublicAdmissionSubmission", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!admissionRateLimitEnabled)
            return RateLimitPartition.GetNoLimiter(partitionKey);

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = admissionPermitLimit,
                Window = TimeSpan.FromSeconds(admissionWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = admissionQueueLimit,
                AutoReplenishment = true
            });
    });

    options.AddClientErrorsPolicy();
    options.AddPasswordResetPolicy();

    // API de plataforma (alta de instituciones): freno a la fuerza bruta de la clave de operador.
    options.AddPolicy("PlatformAdmin", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AcademiaDigital API",
        Version = "v1",
        Description = "API REST para la plataforma AcademiaDigital"
    });

    // Permite probar endpoints protegidos: pegar el token que devuelve /login
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Pega el token JWT obtenido en /api/v1/users/login (sin el prefijo 'Bearer')",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Resolve once at startup so an unsupported mode, missing secret or unsafe
// verification URL fails fast instead of weakening the first public request.
_ = app.Services.GetRequiredService<IAdmissionChallengeVerifier>();

// Multitenant: migra la base de control, registra la institución por defecto y migra las demás.
await app.Services.InitializeTenancyAsync();

// Aplicar migraciones al iniciar con reintentos.
// Aunque docker-compose usa "depends_on: condition: service_healthy" con pg_isready,
// eso solo garantiza que Postgres acepta conexiones, no que la primera conexión desde
// el pool de Npgsql no falle por una condición de carrera al arrancar todos los
// contenedores juntos. Se mantiene un reintento acotado por las dudas.
await using (var scope = app.Services.CreateAsyncScope())
{
    // Base de la institución por defecto (la ya existente). Las demás las migra InitializeTenancyAsync.
    await using var db = scope.ServiceProvider.GetRequiredService<TenantDbContextFactory>()
        .Create(scope.ServiceProvider.GetRequiredService<ITenantDirectory>().Default.DatabaseName);
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
    const int maxAttempts = 10;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.DuplicateTable or PostgresErrorCodes.DuplicateObject)
        {
            // La DB fue creada con EnsureCreated sin historial de migraciones (los objetos ya existen).
            // Se elimina y recrea limpiamente con todas las migraciones aplicadas.
            startupLogger.LogWarning(ex, "DB creada sin historial de migraciones detectada, recreando con migraciones...");
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts
            && (ex is NpgsqlException or TimeoutException or System.Net.Sockets.SocketException))
        {
            // Postgres todavía no acepta conexiones (arranque Docker).
            startupLogger.LogWarning(ex, "Sin conexión a PostgreSQL (intento {Attempt}/{Max}), reintentando en 3s...", attempt, maxAttempts);
            await Task.Delay(3000);
        }
    }
}

// Primero: IP real del cliente y una línea de log por request.
app.UseObservability();

// Swagger y Scalar: habilitados salvo Swagger__Enabled=false (producción).
app.UseApiDocumentation();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<TenantResolutionMiddleware>();
// La sesión vive en la base de la institución: solo se valida donde hay institución resuelta.
app.UseWhen(context => TenantResolutionMiddleware.IsTenantScoped(context.Request.Path),
    branch => branch.UseMiddleware<ActiveSessionMiddleware>());

app.MapControllers();

await app.RunAsync();
