using Amazon;
using Amazon.S3;
using AcademiaDigital.Finance.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDigital.Finance.Infrastructure.Services;

/// <summary>
/// Elige el adaptador de IFileStorage por configuración:
///   Storage:Provider = Local (por defecto) → disco, ReceiptStorage:RootPath (desarrollo y compose local).
///   Storage:Provider = S3                  → bucket Storage:S3:Bucket (AWS, varias instancias).
/// Opcionales de S3: Storage:S3:Prefix, Storage:S3:Region, Storage:S3:ServiceUrl (solo para
/// un S3 compatible local, por ejemplo MinIO en pruebas).
/// </summary>
public static class FileStorageRegistration
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        if (!string.Equals(configuration["Storage:Provider"], "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
            return services;
        }

        services.AddSingleton<IAmazonS3>(_ => CreateClient(configuration));
        services.AddSingleton<IFileStorage, S3FileStorage>();
        return services;
    }

    private static AmazonS3Client CreateClient(IConfiguration configuration)
    {
        var config = new AmazonS3Config();

        var serviceUrl = configuration["Storage:S3:ServiceUrl"];
        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            config.ForcePathStyle = true;
        }
        else if (configuration["Storage:S3:Region"] is { Length: > 0 } region)
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        // Sin claves: en AWS el SDK usa el rol de la instancia (IMDSv2).
        return new AmazonS3Client(config);
    }
}
