using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// Disambiguate vs the surrounding `LankaConnect.BuildingBlocks.Web.ProblemDetails`
// subnamespace which shadows the MVC type when referenced unqualified.
using MvcProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace LankaConnect.BuildingBlocks.Web.ProblemDetails;

/// <summary>
/// Catch-all <see cref="IExceptionHandler"/> that translates unhandled
/// exceptions into RFC 7807 problem-details responses with appropriate HTTP
/// status codes. Maps known exception types to specific status codes;
/// everything else surfaces as <c>500 Internal Server Error</c>.
/// </summary>
/// <remarks>
/// <para>
/// Mapping (specific → general):
/// <list type="bullet">
///   <item><see cref="ValidationException"/> → 400 Bad Request</item>
///   <item><see cref="ArgumentException"/> → 400 Bad Request</item>
///   <item><see cref="UnauthorizedAccessException"/> → 401 Unauthorized</item>
///   <item><see cref="KeyNotFoundException"/> → 404 Not Found</item>
///   <item><see cref="InvalidOperationException"/> → 409 Conflict</item>
///   <item>anything else → 500 Internal Server Error</item>
/// </list>
/// </para>
/// <para>
/// The handler intentionally redacts the exception message from the JSON
/// response on 500s (PII / internal-path risk) — only the type name is
/// surfaced. The full message + stack trace go to the structured log via
/// <see cref="ILogger.LogError(System.Exception?, string, object?[])"/>.
/// </para>
/// </remarks>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Operation conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred"),
        };

        // Always log the full exception with structured context for diagnostics.
        // PII redaction happens at the RESPONSE boundary below, not in the log.
        _logger.LogError(
            exception,
            "Unhandled exception in {Path}: {ExceptionType}",
            httpContext.Request.Path,
            exception.GetType().Name);

        var problem = new MvcProblemDetails
        {
            Status = statusCode,
            Title = title,
            // Only echo the exception message for 4xx (caller-facing errors that
            // are safe to surface — typically validation failures). On 5xx, keep
            // the response generic to avoid leaking internal paths / sensitive data.
            Detail = statusCode is >= 400 and < 500 ? exception.Message : null,
            Instance = httpContext.Request.Path,
            Type = $"https://httpstatuses.com/{statusCode}",
        };

        // For FluentValidation, include the field-level errors as `errors` per
        // RFC 7807 conventions used by ASP.NET Core ValidationProblemDetails.
        if (exception is ValidationException ve)
        {
            var errors = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            problem, cancellationToken: cancellationToken);

        return true; // handled — pipeline stops here
    }
}
