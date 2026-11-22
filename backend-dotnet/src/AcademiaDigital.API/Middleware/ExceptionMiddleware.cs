using System.Text.Json;
using AcademiaDigital.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

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
            var statusCode = await HandleExceptionAsync(context, ex);

            // Solo los 5xx son errores del sistema. Las excepciones de negocio esperadas
            // (credenciales inválidas, conflictos, no encontrado) no se registran como Error:
            // en producción se guardan solo los errores y estas los inundarían.
            if (statusCode >= StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception");
            else
                logger.LogInformation("Handled {ExceptionType} -> {StatusCode}", ex.GetType().Name, statusCode);
        }
    }

    private static async Task<int> HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, msg) = ex switch
        {
            // Domain exceptions
            InvalidCredentialsException e      => (StatusCodes.Status401Unauthorized,  e.Message),
            AcademiaDigital.Application.UseCases.Authentication.GoogleAccountNotRegisteredException e => (StatusCodes.Status404NotFound, e.Message),
            InvalidResetTokenException e       => (StatusCodes.Status400BadRequest,    e.Message),
            InactiveUserException e            => (StatusCodes.Status403Forbidden,      e.Message),
            PendingApprovalException e         => (StatusCodes.Status403Forbidden,      e.Message),
            AccountLockedException e           => (StatusCodes.Status423Locked,         e.Message),
            ForbiddenException e               => (StatusCodes.Status403Forbidden,      e.Message),
            EmailAlreadyExistsException e      => (StatusCodes.Status409Conflict,       e.Message),
            DniAlreadyExistsException e        => (StatusCodes.Status409Conflict,       e.Message),
            AdmissionApplicationAlreadyExistsException e => (StatusCodes.Status409Conflict, e.Message),
            AdmissionFormSlugAlreadyExistsException e => (StatusCodes.Status409Conflict, e.Message),
            AdmissionDivisionAlreadyAssignedException e => (StatusCodes.Status409Conflict, e.Message),
            AdmissionApplicationConcurrencyException e => (StatusCodes.Status409Conflict, e.Message),
            AdmissionChallengeRejectedException e => (StatusCodes.Status403Forbidden, e.Message),
            StudentRematriculationAlreadyExistsException e => (StatusCodes.Status409Conflict, e.Message),
            TeacherAlreadyExistsException e       => (StatusCodes.Status409Conflict,       e.Message),
            UserNotFoundException e            => (StatusCodes.Status404NotFound,       e.Message),
            SessionNotFoundException e         => (StatusCodes.Status404NotFound,       e.Message),
            UnauthorizedUserUpdateException e  => (StatusCodes.Status403Forbidden,      e.Message),
            // Standard exceptions
            KeyNotFoundException e             => (StatusCodes.Status404NotFound,       e.Message),
            ArgumentException e               => (StatusCodes.Status400BadRequest,     e.Message),
            InvalidOperationException e        => (StatusCodes.Status409Conflict,       e.Message),
            DbUpdateException                  => (StatusCodes.Status409Conflict,       "Ya existe una inscripción para este alumno en una de las materias seleccionadas."),
            _                                  => (StatusCodes.Status500InternalServerError, "An internal error occurred")
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new { success = false, msg };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        return statusCode;
    }
}
