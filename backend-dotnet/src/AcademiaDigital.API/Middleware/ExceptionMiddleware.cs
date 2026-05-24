using System.Text.Json;
using AcademiaDigital.Domain.Exceptions;

namespace AcademiaDigital.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, msg) = ex switch
        {
            InvalidCredentialsException e => (StatusCodes.Status401Unauthorized, e.Message),
            InactiveUserException e => (StatusCodes.Status403Forbidden, e.Message),
            AccountLockedException e => (StatusCodes.Status403Forbidden, e.Message),
            EmailAlreadyExistsException e => (StatusCodes.Status409Conflict, e.Message),
            DniAlreadyExistsException e => (StatusCodes.Status409Conflict, e.Message),
            SessionNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            UnauthorizedUserUpdateException e => (StatusCodes.Status403Forbidden, e.Message),
            UserNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
            InvalidUserRoleException e => (StatusCodes.Status400BadRequest, e.Message),
            UnauthorizedRoleChangeException e => (StatusCodes.Status403Forbidden, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An internal error occurred")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new { success = false, msg };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
