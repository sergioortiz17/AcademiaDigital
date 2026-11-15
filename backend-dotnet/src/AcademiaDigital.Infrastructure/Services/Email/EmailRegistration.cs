using Amazon;
using Amazon.SimpleEmailV2;
using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDigital.Infrastructure.Services.Email;

/// <summary>Remitente y región del correo saliente.</summary>
public sealed record EmailOptions(string From);

/// <summary>
/// Elige el emisor de correo por configuración:
///   Email:Provider = Log (por defecto) → solo escribe en el log (desarrollo).
///   Email:Provider = Ses               → Amazon SES v2 con el rol de la instancia.
/// Remitente: Email:From, o MAIL_FROM (lo define Terraform en Beanstalk).
/// Región: Email:Region, o APP_AWS_REGION.
/// </summary>
public static class EmailRegistration
{
    public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        if (!string.Equals(configuration["Email:Provider"], "Ses", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmailSender, LogEmailSender>();
            return services;
        }

        var from = configuration["Email:From"] ?? configuration["MAIL_FROM"]
            ?? throw new InvalidOperationException("Email:From (o MAIL_FROM) es obligatorio con Email:Provider=Ses.");
        var region = configuration["Email:Region"] ?? configuration["APP_AWS_REGION"];

        services.AddSingleton(new EmailOptions(from));
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(_ => string.IsNullOrWhiteSpace(region)
            ? new AmazonSimpleEmailServiceV2Client()
            : new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(region)));
        services.AddSingleton<IEmailSender, SesEmailSender>();
        return services;
    }
}
