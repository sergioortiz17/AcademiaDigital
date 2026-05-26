using AcademiaDigital.API.Middleware;
using AcademiaDigital.Application.UseCases.Admin;
using AcademiaDigital.Application.UseCases.Authentication;
using AcademiaDigital.Application.UseCases.Certificates;
using AcademiaDigital.Application.UseCases.User;
using AcademiaDigital.Infrastructure;
using AcademiaDigital.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

// Certificates
builder.Services.AddScoped<GetCertificateRequestsUseCase>();
builder.Services.AddScoped<CreateCertificateRequestUseCase>();
builder.Services.AddScoped<GetAllCertificateRequestsUseCase>();

// ── CORS ─────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

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

// Aplicar migraciones al iniciar con reintentos para tolerar el startup de SQL Server en Docker
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
    const int maxAttempts = 10;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1801)
        {
            // Error 1801: el motor SQL Server aún está recuperando la DB en Docker.
            // MigrateAsync intentó crearla porque no pudo conectarse aún. Reintentar.
            if (attempt == maxAttempts) throw;
            startupLogger.LogWarning(ex, "SQL Server no está listo (intento {Attempt}/{Max}), reintentando en 3s...", attempt, maxAttempts);
            await Task.Delay(3000);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("already an object named"))
        {
            // La DB fue creada con EnsureCreated sin historial de migraciones.
            // Se elimina y recrea limpiamente con todas las migraciones aplicadas.
            startupLogger.LogWarning(ex, "DB creada con EnsureCreated detectada, recreando con migraciones...");
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
            break;
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (attempt < maxAttempts
            && (ex.Number == -2 || ex.Number == 53 || ex.Number == 40 || ex.Message.Contains("connection")))
        {
            // SQL Server todavía no acepta conexiones (arranque Docker).
            startupLogger.LogWarning(ex, "Sin conexión a SQL Server (intento {Attempt}/{Max}), reintentando en 3s...", attempt, maxAttempts);
            await Task.Delay(3000);
        }
    }
}

// Swagger disponible siempre (no solo en Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcademiaDigital API v1");
    c.RoutePrefix = "swagger";
});

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseMiddleware<ActiveSessionMiddleware>();

app.MapControllers();

await app.RunAsync();
