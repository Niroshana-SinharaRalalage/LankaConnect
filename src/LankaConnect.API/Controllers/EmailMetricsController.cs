using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.Shared.Email.Observability;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 6A.87 Week 3: Email Tracking Dashboard API
/// Provides endpoints for monitoring email sending activity, failures, and migration progress.
/// </summary>
[ApiController]
[Route("api/admin/email-metrics")]
[Produces("application/json")]
public class EmailMetricsController : BaseController<EmailMetricsController>
{
    private readonly IEmailMetrics _emailMetrics;
    private readonly IWebHostEnvironment _environment;

    public EmailMetricsController(
        IMediator mediator,
        ILogger<EmailMetricsController> logger,
        IEmailMetrics emailMetrics,
        IWebHostEnvironment environment)
        : base(mediator, logger)
    {
        _emailMetrics = emailMetrics;
        _environment = environment;
    }

    /// <summary>
    /// Get summary dashboard statistics.
    /// Shows total emails sent, success rate, and failure counts.
    /// </summary>
    /// <returns>Overall email metrics summary</returns>
    [HttpGet("summary")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(typeof(EmailMetricsSummaryResponse), StatusCodes.Status200OK)]
    public IActionResult GetSummary()
    {
        Logger.LogInformation("[Phase 6A.87] Email metrics summary requested");

        var globalStats = _emailMetrics.GetGlobalStats();
        var allTemplateStats = _emailMetrics.GetAllTemplateStats();
        var allHandlerStats = _emailMetrics.GetAllHandlerStats();

        // Calculate migration progress
        var totalHandlers = allHandlerStats.Count;
        var migratedHandlers = allHandlerStats.Values.Count(h => h.TypedParameterPercentage > 0);

        var response = new EmailMetricsSummaryResponse
        {
            TotalEmailsSent = globalStats.TotalEmailsSent,
            TotalSuccesses = globalStats.TotalSuccesses,
            TotalFailures = globalStats.TotalFailures,
            GlobalSuccessRate = Math.Round(globalStats.GlobalSuccessRate, 2),
            TotalTemplatesUsed = allTemplateStats.Count,
            TotalHandlersRecorded = totalHandlers,
            HandlersUsingTypedParameters = migratedHandlers,
            MigrationProgressPercentage = totalHandlers > 0
                ? Math.Round((double)migratedHandlers / totalHandlers * 100, 2)
                : 0,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Get statistics grouped by email template.
    /// Shows success rate, failure count, and average duration for each template.
    /// </summary>
    /// <returns>Per-template metrics</returns>
    [HttpGet("by-template")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(typeof(TemplateMetricsListResponse), StatusCodes.Status200OK)]
    public IActionResult GetByTemplate()
    {
        Logger.LogInformation("[Phase 6A.87] Email metrics by template requested");

        var allStats = _emailMetrics.GetAllTemplateStats();

        var templateMetrics = allStats.Select(kvp => new TemplateMetricsDto
        {
            TemplateName = kvp.Key,
            TotalSent = kvp.Value.TotalSent,
            SuccessCount = kvp.Value.SuccessCount,
            FailureCount = kvp.Value.FailureCount,
            ValidationFailures = kvp.Value.ValidationFailures,
            TemplateNotFoundCount = kvp.Value.TemplateNotFoundCount,
            SuccessRate = Math.Round(kvp.Value.SuccessRate, 2),
            AverageDurationMs = kvp.Value.AverageDurationMs
        })
        .OrderByDescending(t => t.TotalSent)
        .ToList();

        var response = new TemplateMetricsListResponse
        {
            Templates = templateMetrics,
            TotalTemplates = templateMetrics.Count,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Get single template statistics by name.
    /// </summary>
    /// <param name="templateName">Template name (e.g., "template-event-reminder")</param>
    /// <returns>Single template metrics</returns>
    [HttpGet("by-template/{templateName}")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(typeof(TemplateMetricsDto), StatusCodes.Status200OK)]
    public IActionResult GetTemplateByName(string templateName)
    {
        Logger.LogInformation("[Phase 6A.87] Email metrics for template {TemplateName} requested", templateName);

        var stats = _emailMetrics.GetStatsByTemplate(templateName);

        var response = new TemplateMetricsDto
        {
            TemplateName = templateName,
            TotalSent = stats.TotalSent,
            SuccessCount = stats.SuccessCount,
            FailureCount = stats.FailureCount,
            ValidationFailures = stats.ValidationFailures,
            TemplateNotFoundCount = stats.TemplateNotFoundCount,
            SuccessRate = Math.Round(stats.SuccessRate, 2),
            AverageDurationMs = stats.AverageDurationMs
        };

        return Ok(response);
    }

    /// <summary>
    /// Get list of failed email sends for troubleshooting.
    /// Shows correlation ID, template, recipient, and error message.
    /// </summary>
    /// <param name="limit">Maximum number of failures to return (default: 100)</param>
    /// <returns>List of email failures</returns>
    [HttpGet("failures")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(typeof(EmailFailuresResponse), StatusCodes.Status200OK)]
    public IActionResult GetFailures([FromQuery] int limit = 100)
    {
        Logger.LogInformation("[Phase 6A.87] Email failures list requested, limit={Limit}", limit);

        var failures = _emailMetrics.GetFailedEmails()
            .OrderByDescending(f => f.Timestamp)
            .Take(limit)
            .Select(f => new EmailFailureDto
            {
                CorrelationId = f.CorrelationId,
                TemplateName = f.TemplateName,
                RecipientEmail = MaskEmail(f.RecipientEmail),
                ErrorMessage = f.ErrorMessage,
                HandlerName = f.HandlerName,
                Timestamp = f.Timestamp
            })
            .ToList();

        var response = new EmailFailuresResponse
        {
            Failures = failures,
            TotalCount = failures.Count,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Get list of parameter validation failures.
    /// CRITICAL: Shows missing parameters that cause {{Parameter}} literals in emails.
    /// </summary>
    /// <param name="limit">Maximum number of failures to return (default: 100)</param>
    /// <returns>List of validation failures</returns>
    [HttpGet("validation-failures")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(typeof(ValidationFailuresResponse), StatusCodes.Status200OK)]
    public IActionResult GetValidationFailures([FromQuery] int limit = 100)
    {
        Logger.LogInformation("[Phase 6A.87] Validation failures list requested, limit={Limit}", limit);

        var failures = _emailMetrics.GetValidationFailures()
            .OrderByDescending(f => f.Timestamp)
            .Take(limit)
            .Select(f => new ValidationFailureDto
            {
                CorrelationId = f.CorrelationId,
                TemplateName = f.TemplateName,
                MissingParameters = f.MissingParameters,
                HandlerName = f.HandlerName,
                Timestamp = f.Timestamp
            })
            .ToList();

        var response = new ValidationFailuresResponse
        {
            Failures = failures,
            TotalCount = failures.Count,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Get migration progress from Dictionary to typed parameters.
    /// Shows which handlers are using typed parameters vs legacy Dictionary approach.
    /// </summary>
    /// <returns>Migration progress by handler</returns>
    [HttpGet("migration-progress")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(typeof(MigrationProgressResponse), StatusCodes.Status200OK)]
    public IActionResult GetMigrationProgress()
    {
        Logger.LogInformation("[Phase 6A.87] Migration progress requested");

        var allHandlerStats = _emailMetrics.GetAllHandlerStats();

        var handlers = allHandlerStats.Select(kvp => new HandlerMigrationDto
        {
            HandlerName = kvp.Key,
            TotalEmailsSent = kvp.Value.TotalEmailsSent,
            TypedParameterCount = kvp.Value.TypedParameterUsageCount,
            DictionaryParameterCount = kvp.Value.DictionaryParameterUsageCount,
            TypedParameterPercentage = Math.Round(kvp.Value.TypedParameterPercentage, 2),
            IsMigrated = kvp.Value.TypedParameterPercentage > 0
        })
        .OrderByDescending(h => h.TypedParameterPercentage)
        .ToList();

        var totalHandlers = handlers.Count;
        var migratedHandlers = handlers.Count(h => h.IsMigrated);

        var response = new MigrationProgressResponse
        {
            Handlers = handlers,
            TotalHandlers = totalHandlers,
            MigratedHandlers = migratedHandlers,
            MigrationPercentage = totalHandlers > 0
                ? Math.Round((double)migratedHandlers / totalHandlers * 100, 2)
                : 0,
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Reset all metrics (Development/Staging only).
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("reset")]
    [AllowAnonymous] // TODO: Add [Authorize(Policy = "RequireAdmin")] for production
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult ResetMetrics()
    {
        // Only allow in Development or Staging
        if (!_environment.IsDevelopment() && !_environment.IsStaging())
        {
            Logger.LogWarning("[Phase 6A.87] Reset metrics endpoint called in {Environment} - DENIED", _environment.EnvironmentName);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "Metrics reset is only available in Development and Staging environments"
            });
        }

        Logger.LogWarning("[Phase 6A.87] Resetting all email metrics");
        _emailMetrics.ResetMetrics();

        return Ok(new
        {
            message = "Email metrics reset successfully",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Masks email address for privacy (e.g., "jo***@example.com")
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        var localPart = parts[0];
        var domainPart = parts[1];

        var maskedLocal = localPart.Length > 2
            ? localPart[..2] + new string('*', Math.Min(localPart.Length - 2, 3))
            : new string('*', localPart.Length);

        return $"{maskedLocal}@{domainPart}";
    }
}

// ================================================================
// Response DTOs
// ================================================================

/// <summary>
/// Summary dashboard response
/// </summary>
public class EmailMetricsSummaryResponse
{
    public int TotalEmailsSent { get; set; }
    public int TotalSuccesses { get; set; }
    public int TotalFailures { get; set; }
    public double GlobalSuccessRate { get; set; }
    public int TotalTemplatesUsed { get; set; }
    public int TotalHandlersRecorded { get; set; }
    public int HandlersUsingTypedParameters { get; set; }
    public double MigrationProgressPercentage { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Template-specific metrics
/// </summary>
public class TemplateMetricsDto
{
    public string TemplateName { get; set; } = string.Empty;
    public int TotalSent { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int ValidationFailures { get; set; }
    public int TemplateNotFoundCount { get; set; }
    public double SuccessRate { get; set; }
    public int AverageDurationMs { get; set; }
}

/// <summary>
/// List of template metrics
/// </summary>
public class TemplateMetricsListResponse
{
    public List<TemplateMetricsDto> Templates { get; set; } = new();
    public int TotalTemplates { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Email failure record DTO
/// </summary>
public class EmailFailureDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Email failures list response
/// </summary>
public class EmailFailuresResponse
{
    public List<EmailFailureDto> Failures { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Validation failure record DTO
/// </summary>
public class ValidationFailureDto
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public List<string> MissingParameters { get; set; } = new();
    public string HandlerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Validation failures list response
/// </summary>
public class ValidationFailuresResponse
{
    public List<ValidationFailureDto> Failures { get; set; } = new();
    public int TotalCount { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Handler migration status DTO
/// </summary>
public class HandlerMigrationDto
{
    public string HandlerName { get; set; } = string.Empty;
    public int TotalEmailsSent { get; set; }
    public int TypedParameterCount { get; set; }
    public int DictionaryParameterCount { get; set; }
    public double TypedParameterPercentage { get; set; }
    public bool IsMigrated { get; set; }
}

/// <summary>
/// Migration progress response
/// </summary>
public class MigrationProgressResponse
{
    public List<HandlerMigrationDto> Handlers { get; set; } = new();
    public int TotalHandlers { get; set; }
    public int MigratedHandlers { get; set; }
    public double MigrationPercentage { get; set; }
    public DateTime Timestamp { get; set; }
}
