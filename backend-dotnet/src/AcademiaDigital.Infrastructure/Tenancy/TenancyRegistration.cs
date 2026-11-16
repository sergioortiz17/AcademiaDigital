using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Infrastructure.Persistence;
using AcademiaDigital.Infrastructure.Persistence.Control;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDigital.Infrastructure.Tenancy;

public static class TenancyRegistration
{
    /// <summary>
    /// Registra el multitenant: el catálogo en el esquema "control" y, por request, un
    /// <see cref="AppDbContext"/> apuntando al esquema de la institución resuelta (base compartida).
    /// </summary>
    public static IServiceCollection AddTenancy(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenancyOptions>(configuration.GetSection("Tenancy"));
        services.AddSingleton<TenantConnectionStrings>();
        services.AddSingleton<TenantDbContextFactory>();

        services.AddDbContext<ControlDbContext>((sp, options) =>
            options.UseSchemaNpgsql(sp.GetRequiredService<TenantConnectionStrings>().Control));

        services.AddSingleton<ITenantDirectory, TenantDirectory>();
        services.AddScoped<ITenantRegistry, TenantRegistry>();
        services.AddScoped<IPlatformRequestStore, PlatformRequestStore>();
        services.AddScoped<ITenantProvisioner, TenantProvisioner>();

        // Misma instancia scoped bajo dos tipos: el middleware escribe TenantContext, el resto lee ITenantContext.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connections = sp.GetRequiredService<TenantConnectionStrings>();
            string schema;
            try
            {
                schema = sp.GetRequiredService<ITenantContext>().Current.SchemaName;
            }
            catch (InvalidOperationException) when (IsEfCoreTool)
            {
                // `dotnet ef` resuelve el contexto sin request: usa el esquema por defecto.
                // En ejecución normal sin institución resuelta se falla (nunca se asume un esquema).
                schema = TenantConnectionStrings.DefaultSchemaName;
            }
            options.UseSchemaNpgsql(connections.For(schema));
        });

        return services;
    }

    private static bool IsEfCoreTool
        => string.Equals(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name, "ef", StringComparison.Ordinal);
}
