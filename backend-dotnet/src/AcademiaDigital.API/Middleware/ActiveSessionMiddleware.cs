using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using System.Security.Claims;

namespace AcademiaDigital.API.Middleware;

/// <summary>
/// Valida que el JWT token del header Authorization corresponda
/// a una sesión activa en la base de datos.
/// Acepta el token con o sin el prefijo "Bearer ".
/// </summary>
public class ActiveSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        ITokenService tokenService,
        ISessionRepository sessionRepository)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(authHeader))
        {
            // Strip "Bearer " prefix if present (standard HTTP Authorization header)
            var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..]
                : authHeader;

            if (tokenService.ValidateToken(token))
            {
                var session = await sessionRepository.FindByTokenAsync(token);

                if (session != null && session.User.IsActive)
                {
                    context.Items["UserId"] = session.UserId;
                    context.Items["IsSuperuser"] = false;

                    var claims = new[]
                    {
                        new Claim("id", session.UserId.ToString()),
                        new Claim(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                        new Claim(ClaimTypes.Role, session.User.Role.ToString())
                    };

                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity(claims, "ActiveSession"));
                }
                else
                {
                    context.Items.Remove("UserId");
                    context.User = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
        }

        await next(context);
    }
}
