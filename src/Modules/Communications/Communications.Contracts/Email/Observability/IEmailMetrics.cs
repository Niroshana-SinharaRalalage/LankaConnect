namespace LankaConnect.Shared.Email.Observability;

/// <summary>
/// Phase 6A.86: Metrics collection interface for email operations.
///
/// Purpose (Option 3 Integration):
/// - Feed data to metrics dashboard showing email send rates, success rates, failures
/// - Track template-specific metrics (which templates fail most? which are slowest?)
/// - Monitor hybrid system rollout (how many handlers using typed vs Dictionary parameters?)
/// - Alert on anomalies (sudden spike in failures, validation errors)
///
/// Dashboard Metrics:
/// - Total emails sent (hourly, daily, weekly)
/// - Success rate by template (95%+ target)
/// - Average send duration by template (under 500ms target)
/// - Parameter validation failure rate (0% target after hybrid migration)
/// - Template not found errors (0 expected - alerts on ANY occurrence)
/// - Typed vs Dictionary parameter usage (tracks migration progress)
///
/// Alerting Thresholds:
/// - Template not found: ANY occurrence → Critical alert
/// - Success rate drops below 95%: Warning alert
/// - Validation failures > 5% of sends: Warning alert
/// - Average duration > 1000ms: Performance alert
/// </summary>
public interface IEmailMetrics
{
    /// <summary>
    /// Records an email send attempt with duration and success/failure.
    /// </summary>
    /// <param name="templateName">Template used for this email</param>
    /// <param name="durationMs">Time taken to send email</param>
    /// <param name="success">Whether email sent successfully</param>
    void RecordEmailSent(string templateName, int durationMs, bool success);

    /// <summary>
    /// Records a parameter validation failure.
    /// CRITICAL: Tracks the literal {{Parameter}} issues we're trying to eliminate.
    /// </summary>
    /// <param name="templateName">Template with invalid parameters</param>
    void RecordParameterValidationFailure(string templateName);

    /// <summary>
    /// Records a template not found error.
    /// CRITICAL: Alerts to template name mismatches (like member-email-verification issue).
    /// </summary>
    /// <param name="templateName">Template name that was not found</param>
    void RecordTemplateNotFound(string templateName);

    /// <summary>
    /// Records handler usage for tracking hybrid system rollout progress.
    /// </summary>
    /// <param name="handlerName">Handler class name</param>
    /// <param name="usedTypedParameters">True if handler used typed parameters, false if Dictionary</param>
    void RecordHandlerUsage(string handlerName, bool usedTypedParameters);

    /// <summary>
    /// Gets metrics for a specific email template.
    /// Used by dashboard to show per-template statistics.
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <returns>Template-specific metrics</returns>
    TemplateMetrics GetStatsByTemplate(string templateName);

    /// <summary>
    /// Gets metrics for a specific handler.
    /// Used by dashboard to track staged rollout progress.
    /// </summary>
    /// <param name="handlerName">Handler class name</param>
    /// <returns>Handler-specific metrics</returns>
    HandlerMetrics GetStatsByHandler(string handlerName);

    /// <summary>
    /// Gets global metrics aggregated across all templates and handlers.
    /// Used by dashboard homepage showing overall system health.
    /// </summary>
    /// <returns>Global aggregated metrics</returns>
    GlobalMetrics GetGlobalStats();

    /// <summary>
    /// Resets all metrics (primarily for testing, but could be used for daily/weekly resets).
    /// </summary>
    void ResetMetrics();

    // ================================================================
    // Phase 6A.87 Week 3: Dashboard API support methods
    // ================================================================

    /// <summary>
    /// Gets metrics for ALL recorded templates.
    /// Used by dashboard to show per-template statistics table.
    /// </summary>
    /// <returns>Dictionary of template name to metrics</returns>
    IReadOnlyDictionary<string, TemplateMetrics> GetAllTemplateStats();

    /// <summary>
    /// Gets metrics for ALL recorded handlers.
    /// Used by dashboard to track staged rollout progress across all handlers.
    /// </summary>
    /// <returns>Dictionary of handler name to metrics</returns>
    IReadOnlyDictionary<string, HandlerMetrics> GetAllHandlerStats();

    /// <summary>
    /// Records a failed email send with detailed information for troubleshooting.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="templateName">Template that failed</param>
    /// <param name="recipientEmail">Intended recipient</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="handlerName">Handler that initiated the send</param>
    void RecordFailedEmail(string correlationId, string templateName, string recipientEmail, string errorMessage, string handlerName);

    /// <summary>
    /// Gets list of failed email sends for dashboard display.
    /// </summary>
    /// <returns>List of failed email records</returns>
    IReadOnlyList<EmailFailureRecord> GetFailedEmails();

    /// <summary>
    /// Records a parameter validation failure with details about missing parameters.
    /// CRITICAL: Tracks the literal {{Parameter}} issues we're trying to eliminate.
    /// </summary>
    /// <param name="correlationId">Correlation ID for tracing</param>
    /// <param name="templateName">Template with validation failure</param>
    /// <param name="missingParameters">List of missing/invalid parameters</param>
    /// <param name="handlerName">Handler that had the validation error</param>
    void RecordValidationFailureDetails(string correlationId, string templateName, List<string> missingParameters, string handlerName);

    /// <summary>
    /// Gets list of parameter validation failures for dashboard display.
    /// </summary>
    /// <returns>List of validation failure records</returns>
    IReadOnlyList<ValidationFailureRecord> GetValidationFailures();
}

/// <summary>
/// Metrics for a specific email template.
/// </summary>
public class TemplateMetrics
{
    public int TotalSent { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int ValidationFailures { get; set; }
    public int TemplateNotFoundCount { get; set; }
    public int AverageDurationMs { get; set; }
    public int TotalDurationMs { get; set; }

    /// <summary>
    /// Success rate as percentage (0-100)
    /// </summary>
    public double SuccessRate => TotalSent > 0 ? (double)SuccessCount / TotalSent * 100 : 0;
}

/// <summary>
/// Metrics for a specific email handler.
/// Tracks hybrid system rollout progress.
/// </summary>
public class HandlerMetrics
{
    public int TypedParameterUsageCount { get; set; }
    public int DictionaryParameterUsageCount { get; set; }
    public int TotalEmailsSent { get; set; }

    /// <summary>
    /// Percentage of emails sent using typed parameters (0-100)
    /// Tracks migration progress: 0% = not started, 50% = halfway, 100% = complete
    /// </summary>
    public double TypedParameterPercentage =>
        TotalEmailsSent > 0 ? (double)TypedParameterUsageCount / TotalEmailsSent * 100 : 0;
}

/// <summary>
/// Global metrics aggregated across all templates and handlers.
/// </summary>
public class GlobalMetrics
{
    public int TotalEmailsSent { get; set; }
    public int TotalSuccesses { get; set; }
    public int TotalFailures { get; set; }

    /// <summary>
    /// Overall success rate as percentage (0-100)
    /// Target: 95%+
    /// </summary>
    public double GlobalSuccessRate =>
        TotalEmailsSent > 0 ? (double)TotalSuccesses / TotalEmailsSent * 100 : 0;
}

// ================================================================
// Phase 6A.87 Week 3: Dashboard API support records
// ================================================================

/// <summary>
/// Record of a failed email send attempt.
/// Used by dashboard to display failure list for troubleshooting.
/// </summary>
public class EmailFailureRecord
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string HandlerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Record of a parameter validation failure.
/// Used by dashboard to track and alert on {{Parameter}} issues.
/// </summary>
public class ValidationFailureRecord
{
    public string CorrelationId { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public List<string> MissingParameters { get; set; } = new();
    public string HandlerName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
