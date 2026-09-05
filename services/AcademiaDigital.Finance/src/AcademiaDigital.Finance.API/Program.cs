using AcademiaDigital.Finance.Application.UseCases.Finance;
using AcademiaDigital.Finance.Application.UseCases.Payments;
using AcademiaDigital.Finance.Application.UseCases.Receipts;
using AcademiaDigital.Finance.Domain.Entities;
using AcademiaDigital.Finance.Infrastructure;
using AcademiaDigital.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure (FinanceDbContext, repositories, policies, services, directory) ──
builder.Services.AddFinanceInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

// ── Application handlers ─────────────────────────────────────────────────────
// Finance
builder.Services.AddScoped<GetFinancialConceptsQueryHandler>();
builder.Services.AddScoped<CreateFinancialConceptCommandHandler>();
builder.Services.AddScoped<UpdateFinancialConceptCommandHandler>();
builder.Services.AddScoped<GetFinancialRatesQueryHandler>();
builder.Services.AddScoped<UpsertFinancialRateCommandHandler>();
builder.Services.AddScoped<GetFinancialBenefitsQueryHandler>();
builder.Services.AddScoped<CreateFinancialBenefitCommandHandler>();
builder.Services.AddScoped<GetBillingPlansQueryHandler>();
builder.Services.AddScoped<CreateBillingPlanCommandHandler>();
builder.Services.AddScoped<GenerateStudentDebtsCommandHandler>();
builder.Services.AddScoped<GetStudentDebtsQueryHandler>();
builder.Services.AddScoped<GetStudentDebtSummaryQueryHandler>();
// Payments
builder.Services.AddScoped<GetPaymentMethodsQueryHandler>();
builder.Services.AddScoped<CreatePaymentCommandHandler>();
builder.Services.AddScoped<ConfirmPaymentCommandHandler>();
builder.Services.AddScoped<ReconcilePaymentCommandHandler>();
builder.Services.AddScoped<ReversePaymentCommandHandler>();
builder.Services.AddScoped<GetPaymentsQueryHandler>();
// Receipts
builder.Services.AddScoped<ReceiptWorkflowService>();
builder.Services.AddScoped<GetReceiptsQueryHandler>();
builder.Services.AddScoped<GetReceiptQueryHandler>();
builder.Services.AddScoped<DownloadReceiptQueryHandler>();

// ── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger + Scalar ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AcademiaDigital Finance API",
        Version = "v1",
        Description = "Microservicio de finanzas (conceptos, aranceles, deudas, pagos, recibos)"
    });
});

var app = builder.Build();

// ── Bootstrap: asegurar que la BASE de datos propia de Finance existe ────────
// Finance vive en su PROPIA base (academiadigital_finance) dentro de la misma instancia
// Postgres que el monolito. Esto garantiza aislamiento REAL: el monolito y Finance no
// comparten base, así una recreación del esquema del monolito jamás puede tocar los datos
// de Finance (ADR 0001). Como POSTGRES_DB solo crea una base al iniciar el contenedor,
// creamos la de Finance acá si falta, conectándonos a la base de mantenimiento 'postgres'.
await EnsureFinanceDatabaseExistsAsync(builder.Configuration, app.Services);

// ── Migrate on startup with Npgsql retries (Docker startup race) ─────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<FinanceDbContext>>();
    const int maxAttempts = 10;

    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            // El schema 'finance' debe existir antes de migrar (HasDefaultSchema + MigrationsHistoryTable
            // apuntan a 'finance'); se crea idempotente.
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS finance;");
            await db.Database.MigrateAsync();
            await SeedPaymentMethodsAsync(db);
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts
            && (ex is NpgsqlException or TimeoutException or System.Net.Sockets.SocketException))
        {
            startupLogger.LogWarning(ex, "Sin conexión a PostgreSQL (intento {Attempt}/{Max}), reintentando en 3s...", attempt, maxAttempts);
            await Task.Delay(3000);
        }
        // NO hay fallback destructivo (EnsureDeleted): en producción nunca auto-borramos datos.
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcademiaDigital Finance API v1");
    c.RoutePrefix = "swagger";
});
app.MapScalarApiReference("/scalar", options =>
{
    options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
});

app.MapControllers();

await app.RunAsync();

// Crea la base de datos propia de Finance si no existe, conectándose a la base de
// mantenimiento 'postgres' de la misma instancia. Idempotente y tolerante a que Postgres
// todavía no acepte conexiones (reintenta). No usa EF (CREATE DATABASE no va en transacción).
static async Task EnsureFinanceDatabaseExistsAsync(IConfiguration config, IServiceProvider services)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    var connString = config.GetConnectionString("DefaultConnection")!;
    var target = new NpgsqlConnectionStringBuilder(connString);
    var dbName = target.Database!;

    var admin = new NpgsqlConnectionStringBuilder(connString) { Database = "postgres" };

    for (var attempt = 1; attempt <= 10; attempt++)
    {
        try
        {
            await using var conn = new NpgsqlConnection(admin.ConnectionString);
            await conn.OpenAsync();
            await using (var check = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @n", conn))
            {
                check.Parameters.AddWithValue("n", dbName);
                if (await check.ExecuteScalarAsync() is not null) return; // ya existe
            }
            // CREATE DATABASE no admite parámetros ni transacción; el nombre viene de config, no de input externo.
            await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{dbName}\"", conn))
                await create.ExecuteNonQueryAsync();
            logger.LogInformation("Finance: base de datos '{Db}' creada.", dbName);
            return;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.DuplicateDatabase)
        {
            return; // otra instancia la creó primero (carrera de arranque)
        }
        catch (Exception ex) when (attempt < 10 && ex is NpgsqlException or TimeoutException or System.Net.Sockets.SocketException)
        {
            logger.LogWarning("Finance: esperando Postgres para bootstrap de la base (intento {Attempt}/10)...", attempt);
            await Task.Delay(3000);
        }
    }
}

// Idempotent seed of the 4 catalogue payment methods. The migration seeds them via HasData;
// this is a safety net so a manually-created/empty schema still ends up with the catalogue.
static async Task SeedPaymentMethodsAsync(FinanceDbContext db)
{
    var seed = new (int Id, string Code, string Name, PaymentMethodKind Kind, int Order)[]
    {
        (1, "CASH", "Efectivo", PaymentMethodKind.Cash, 1),
        (2, "BANK_TRANSFER", "Transferencia bancaria", PaymentMethodKind.BankTransfer, 2),
        (3, "DEBIT_CARD", "Tarjeta de débito", PaymentMethodKind.DebitCard, 3),
        (4, "CREDIT_CARD", "Tarjeta de crédito", PaymentMethodKind.CreditCard, 4),
    };
    var existing = await db.PaymentMethods.Select(m => m.Id).ToListAsync();
    var missing = seed.Where(s => !existing.Contains(s.Id))
        .Select(s => new PaymentMethod { Id = s.Id, Code = s.Code, Name = s.Name, Kind = s.Kind, IsActive = true, DisplayOrder = s.Order })
        .ToArray();
    if (missing.Length == 0) return;
    db.PaymentMethods.AddRange(missing);
    await db.SaveChangesAsync();
}

public partial class Program;
