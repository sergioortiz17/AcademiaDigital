using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AcademiaDigital.API.Filters;

/// <summary>
/// Protege la API de plataforma (alta de instituciones) con una clave de operador en el header
/// <c>X-Platform-Key</c>, comparada con <c>Platform:AdminKey</c>. Es independiente de los usuarios
/// de cada institución: ningún Admin de un instituto puede crear otros institutos.
/// Sin <c>Platform:AdminKey</c> configurada la API de plataforma queda deshabilitada (404).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PlatformAdminKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Platform-Key";
    private const int MinKeyLength = 16;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configured = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Platform:AdminKey"];
        if (string.IsNullOrEmpty(configured) || configured.Length < MinKeyLength)
        {
            context.Result = new NotFoundResult();
            return;
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
        var expectedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        var providedBytes = SHA256.HashData(Encoding.UTF8.GetBytes(provided));
        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes))
            context.Result = new ObjectResult(new { success = false, msg = "Clave de plataforma inválida." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
    }
}
