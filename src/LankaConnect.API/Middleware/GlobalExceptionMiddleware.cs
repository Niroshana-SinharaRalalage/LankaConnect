using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LankaConnect.API.Middleware;

/// <summary>
/// Phase 6A.X: Global exception handling middleware with comprehensive logging
/// Catches all unhandled exceptions and returns user-friendly error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> _logger,
        IHostEnvironment environment)
    {
        _next = next;
        this._logger = _logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Extract request context for logging
        var requestId = context.Response.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        var userId = context.User.FindFirst("sub")?.Value
            ?? context.User.FindFirst("id")?.Value;
        var userName = context.User.Identity?.Name;

        // Determine error type and response
        var (statusCode, errorType, userMessage, developerMessage) = GetErrorDetails(exception);

        // Log with full context
        _logger.LogError(exception,
            "GLOBAL EXCEPTION HANDLER: {ErrorType} occurred in {Method} {Path}. " +
            "User: {UserId} ({UserName}), RequestId: {RequestId}, " +
            "StatusCode: {StatusCode}, Message: {Message}",
            errorType,
            context.Request.Method,
            context.Request.Path,
            userId ?? "Anonymous",
            userName ?? "Anonymous",
            requestId,
            statusCode,
            exception.Message);

        // Build error response
        var errorResponse = new ErrorResponse
        {
            StatusCode = statusCode,
            ErrorType = errorType,
            Message = userMessage,
            RequestId = requestId,
            Timestamp = DateTime.UtcNow
        };

        // Include detailed error in development only
        if (_environment.IsDevelopment())
        {
            errorResponse.DeveloperMessage = developerMessage;
            errorResponse.StackTrace = exception.StackTrace;
        }

        // Set response
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, jsonOptions));
    }

    private (int StatusCode, string ErrorType, string UserMessage, string DeveloperMessage) GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            // Database errors
            DbUpdateException dbEx => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ErrorType: "DatabaseError",
                UserMessage: "A database error occurred. Please try again later.",
                DeveloperMessage: $"DbUpdateException: {dbEx.InnerException?.Message ?? dbEx.Message}"
            ),

            NpgsqlException npgsqlEx when npgsqlEx.SqlState == "42703" => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ErrorType: "DatabaseConfigurationError",
                UserMessage: "A database configuration error occurred. Please contact support.",
                DeveloperMessage: $"PostgreSQL column does not exist: {npgsqlEx.Message}. " +
                                  "This indicates EF Core configuration (HasColumnName) doesn't match database schema."
            ),

            NpgsqlException npgsqlEx => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ErrorType: "DatabaseError",
                UserMessage: "A database error occurred. Please try again later.",
                DeveloperMessage: $"PostgreSQL Error (SqlState: {npgsqlEx.SqlState}): {npgsqlEx.Message}"
            ),

            // Validation errors
            InvalidOperationException invalidOpEx when invalidOpEx.Message.Contains("Configuration validation failed") => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ErrorType: "ConfigurationError",
                UserMessage: "Application configuration error. Please contact support.",
                DeveloperMessage: invalidOpEx.Message
            ),

            InvalidOperationException invalidOpEx => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ErrorType: "InvalidOperation",
                UserMessage: "The requested operation is invalid.",
                DeveloperMessage: invalidOpEx.Message
            ),

            ArgumentNullException argNullEx => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ErrorType: "InvalidArgument",
                UserMessage: "Required information is missing.",
                DeveloperMessage: $"ArgumentNullException: {argNullEx.ParamName} - {argNullEx.Message}"
            ),

            ArgumentException argEx => (
                StatusCode: (int)HttpStatusCode.BadRequest,
                ErrorType: "InvalidArgument",
                UserMessage: "Invalid information provided.",
                DeveloperMessage: $"ArgumentException: {argEx.ParamName} - {argEx.Message}"
            ),

            // Unauthorized/Forbidden
            UnauthorizedAccessException => (
                StatusCode: (int)HttpStatusCode.Forbidden,
                ErrorType: "Forbidden",
                UserMessage: "You do not have permission to perform this action.",
                DeveloperMessage: "UnauthorizedAccessException"
            ),

            // HTTP request errors (external API calls)
            HttpRequestException httpEx => (
                StatusCode: (int)HttpStatusCode.BadGateway,
                ErrorType: "ExternalServiceError",
                UserMessage: "An external service is currently unavailable. Please try again later.",
                DeveloperMessage: $"HttpRequestException: {httpEx.Message}"
            ),

            TaskCanceledException taskCancelledEx when taskCancelledEx.InnerException is TimeoutException => (
                StatusCode: (int)HttpStatusCode.GatewayTimeout,
                ErrorType: "Timeout",
                UserMessage: "The request timed out. Please try again.",
                DeveloperMessage: "TaskCanceledException: Request timeout"
            ),

            TimeoutException => (
                StatusCode: (int)HttpStatusCode.RequestTimeout,
                ErrorType: "Timeout",
                UserMessage: "The request timed out. Please try again.",
                DeveloperMessage: "TimeoutException"
            ),

            // Default
            _ => (
                StatusCode: (int)HttpStatusCode.InternalServerError,
                ErrorType: "InternalServerError",
                UserMessage: "An unexpected error occurred. Please try again later.",
                DeveloperMessage: $"{exception.GetType().Name}: {exception.Message}"
            )
        };
    }

    private class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? DeveloperMessage { get; set; }
        public string? StackTrace { get; set; }
    }
}
