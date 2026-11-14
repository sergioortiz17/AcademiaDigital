using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AcademiaDigital.Finance.Domain.Exceptions;

namespace AcademiaDigital.Finance.API.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled Finance exception");
            var (status, message) = exception switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, exception.Message),
                ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, exception.Message),
                InvalidOperationException => (StatusCodes.Status409Conflict, exception.Message),
                DbUpdateException => (StatusCodes.Status409Conflict, "The financial operation conflicts with existing data."),
                _ => (StatusCodes.Status500InternalServerError, "An internal Finance error occurred.")
            };

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, new
            {
                success = false,
                msg = message
            }, cancellationToken: context.RequestAborted);
        }
    }
}
