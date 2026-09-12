using AcademiaDigital.Finance.Application.Interfaces;
using AcademiaDigital.Finance.Domain.Interfaces.Repositories;
using AcademiaDigital.Finance.Domain.Services;
using AcademiaDigital.Finance.Infrastructure.Persistence;
using AcademiaDigital.Finance.Infrastructure.Persistence.Repositories;
using AcademiaDigital.Finance.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDigital.Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + PostgreSQL (schema 'finance', configured via FinanceDbContext.OnModelCreating).
        services.AddDbContext<FinanceDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                // La tabla de historial de migraciones vive en el schema 'finance' (no en public),
                // así el migrador la busca/crea de forma consistente con HasDefaultSchema("finance").
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "finance")));

        // Repositories + unit of work
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Domain policies
        services.AddScoped<FinancePolicy>();
        services.AddScoped<PaymentPolicy>();

        // Services
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IReceiptPdfGenerator, SimpleReceiptPdfGenerator>();

        // Directory client (display names from the monolito, degrades to id).
        services.AddMemoryCache();
        var monolithBaseUrl = configuration["Monolith:BaseUrl"] ?? "http://backend:8000";
        services.AddHttpClient<IDirectoryClient, HttpDirectoryClient>(client =>
        {
            client.BaseAddress = new Uri(monolithBaseUrl.EndsWith('/') ? monolithBaseUrl : monolithBaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

        return services;
    }
}
