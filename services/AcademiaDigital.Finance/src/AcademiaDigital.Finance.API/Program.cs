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
            // El schema 'finance' debe existir ANTES de migrar: con SearchPath=finance +
            // HasDefaultSchema("finance"), EF busca finance.__EFMigrationsHistory y falla con
            // 42P01 si el schema no está creado. Lo creamos de forma idempotente primero.
            await db.Database.ExecuteSqlRawAsync("CREATE SCHEMA IF NOT EXISTS finance;");
            await db.Database.MigrateAsync();
            await SeedPaymentMethodsAsync(db);
            break;
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.DuplicateTable or PostgresErrorCodes.DuplicateObject)
        {
            startupLogger.LogWarning(ex, "Finance DB creada sin historial de migraciones detectada, recreando con migraciones...");
            await db.Database.EnsureDeletedAsync();
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
