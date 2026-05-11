using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestApiDdd.Domain.Common;
using RestApiDdd.Service.Exceptions;

namespace RestApiDdd.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ApplicationValidationException => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
            DatabaseConnectionException => (StatusCodes.Status503ServiceUnavailable, "Database unavailable", exception.Message),
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violation", exception.Message),
            DbUpdateException => (StatusCodes.Status409Conflict, "Database update failed", "The requested change conflicts with persisted data."),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.")
        };

        if (ShouldLogError(exception))
        {
            logger.LogError(exception, "Request failed with unexpected exception.");
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ApplicationValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static bool ShouldLogError(Exception exception)
    {
        return exception is not NotFoundException
            and not ConflictException
            and not ApplicationValidationException
            and not DomainException;
    }
}
