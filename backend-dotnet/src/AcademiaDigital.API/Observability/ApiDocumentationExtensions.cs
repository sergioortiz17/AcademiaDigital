using Scalar.AspNetCore;

namespace AcademiaDigital.API.Observability;

/// <summary>
/// Swagger y Scalar. Habilitados por defecto (desarrollo, docker-compose local y la suite
/// de regresión los usan); en producción Terraform define Swagger__Enabled=false para no
/// publicar el mapa completo de la API en internet.
/// </summary>
public static class ApiDocumentationExtensions
{
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        if (!app.Configuration.GetValue("Swagger:Enabled", true))
            return app;

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcademiaDigital API v1");
            c.RoutePrefix = "swagger";
        });

        // Scalar convive con Swagger durante la migración del equipo: reutiliza el mismo swagger.json
        app.MapScalarApiReference("/scalar", options =>
        {
            options.OpenApiRoutePattern = "/swagger/v1/swagger.json";
        });

        return app;
    }
}
