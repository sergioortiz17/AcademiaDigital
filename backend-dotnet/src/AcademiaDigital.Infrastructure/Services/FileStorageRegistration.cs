using Amazon;
using Amazon.S3;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Infrastructure.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDigital.Infrastructure.Services;

/// <summary>
/// Elige el adaptador de IFileStorage por configuración:
///   Storage:Provider = Local (por defecto) → disco, AdmissionStorage:RootPath (desarrollo y compose local).
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
            services.AddSingleton<LocalFileStorage>();
            services.AddScoped<IFileStorage>(sp => new TenantFileStorage(
                sp.GetRequiredService<LocalFileStorage>(), sp.GetRequiredService<ITenantContext>()));
            // Sin S3: los documentos de los alumnos van al mismo disco local.
            services.AddScoped<IStudentDocumentStorage>(sp => new StudentDocumentStorage(sp.GetRequiredService<IFileStorage>()));
            return services;
        }

        services.AddSingleton<IAmazonS3>(_ => CreateClient(configuration));
        services.AddSingleton(sp => new S3FileStorage(sp.GetRequiredService<IAmazonS3>(), configuration));
        services.AddScoped<IFileStorage>(sp => new TenantFileStorage(
            sp.GetRequiredService<S3FileStorage>(), sp.GetRequiredService<ITenantContext>()));

        // Requisitos de ingreso: bucket propio (Storage:StudentDocuments:Bucket) para que
        // Secretaría los descargue sin mezclarlos con certificados y recibos. Si no está
        // configurado, se guardan en el bucket general.
        var studentBucket = configuration["Storage:StudentDocuments:Bucket"];
        services.AddScoped<IStudentDocumentStorage>(sp => new StudentDocumentStorage(
            string.IsNullOrWhiteSpace(studentBucket)
                ? sp.GetRequiredService<IFileStorage>()
                : new TenantFileStorage(
                    new S3FileStorage(sp.GetRequiredService<IAmazonS3>(), studentBucket, configuration["Storage:StudentDocuments:Prefix"]),
                    sp.GetRequiredService<ITenantContext>())));
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
