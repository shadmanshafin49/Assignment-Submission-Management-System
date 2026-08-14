using System.Text.Json;
using Ams.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Ams.Api.Middleware;

/// <summary>
/// Translates domain exceptions into RFC 7807 ProblemDetails responses. Expected, rule-driven
/// failures are logged at Warning with their status; anything unexpected is logged at Error and
/// returned as a generic 500 so internal details never reach the client.
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail, errorCode) = exception switch
        {
            NotFoundException e =>
                (StatusCodes.Status404NotFound, "Resource not found", e.Message, e.ErrorCode),

            ForbiddenException e =>
                (StatusCodes.Status403Forbidden, "Access denied", e.Message, e.ErrorCode),

            BusinessRuleException e =>
                (StatusCodes.Status409Conflict, "Request conflicts with a business rule", e.Message, e.ErrorCode),

            ValidationFailedException e =>
                (StatusCodes.Status400BadRequest, "Validation failed", e.Message, e.ErrorCode),

            FluentValidation.ValidationException e =>
                (StatusCodes.Status400BadRequest, "Validation failed", e.Message, "validation_failed"),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                  // Never leak internals in production.
                  environment.IsDevelopment() ? exception.ToString() : "Please try again later.",
                  "internal_error")
        };

        if (status >= 500)
        {
            logger.LogError(exception,
                "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(
                "{Status} on {Method} {Path}: {Message}",
                status, context.Request.Method, context.Request.Path, exception.Message);
        }

        if (context.Response.HasStarted)
        {
            logger.LogWarning("Response already started; cannot write ProblemDetails");
            return;
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
