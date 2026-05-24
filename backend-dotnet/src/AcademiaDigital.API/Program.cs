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
builder.Services.AddScoped<UpdateUserUseCase>();

// Admin
builder.Services.AddScoped<GetUsersUseCase>();
builder.Services.AddScoped<UpdateUserRoleUseCase>();
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

// Aplicar migraciones pendientes al iniciar (crea la DB si no existe)
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("already an object named"))
    {
        // La DB fue creada con EnsureCreated sin historial de migraciones.
        // Se elimina y recrea limpiamente con todas las migraciones aplicadas.
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
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
