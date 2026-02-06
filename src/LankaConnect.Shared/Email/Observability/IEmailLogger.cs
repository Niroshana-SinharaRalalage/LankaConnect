namespace LankaConnect.Shared.Email.Observability;

/// <summary>
/// Phase 6A.86: Structured logging interface for email operations.
///
/// Purpose:
/// - Provide correlation IDs for tracing email sends end-to-end
/// - Log parameter validation failures before they cause production issues
/// - Track template rendering performance
/// - Monitor feature flag usage during staged rollout
///
/// Correlation ID Flow:
/// 1. Handler generates correlation ID at start
/// 2. All logs include correlation ID for traceability
/// 3. Correlation ID stored with sent email record for debugging
/// 4. Azure Application Insights groups logs by correlation ID
///
/// Integration with Option 3 (Metrics Dashboard):
/// - Logs feed into Application Insights
/// - Custom queries aggregate logs for dashboard
/// - Real-time alerting configured on ERROR/WARNING logs
/// </summary>
public interface IEmailLogger
{
    /// <summary>
    /// Logs the start of an email send operation.
    /// Called at the beginning of every email send attempt.
    /// </summary>
    /// <param name="correlationId">Unique ID for this email send operation</param>
    /// <param name="templateName">Email template being used</param>
    /// <param name="recipientEmail">Recipient's email address</param>
    void LogEmailSendStart(string correlationId, string templateName, string recipientEmail);

    /// <summary>
    /// Logs successful email send with performance metrics.
    /// Called after email successfully queued/sent.
    /// </summary>
    /// <param name="correlationId">Correlation ID from start</param>
    /// <param name="templateName">Email template used</param>
    /// <param name="durationMs">Time taken to send email in milliseconds</param>
    void LogEmailSendSuccess(string correlationId, string templateName, int durationMs);

    /// <summary>
    /// Logs email send failure with exception details.
    /// Called when email send fails (SMTP error, template not found, etc.)
    /// </summary>
    /// <param name="correlationId">Correlation ID from start</param>
    /// <param name="templateName">Email template attempted</param>
    /// <param name="errorMessage">Human-readable error description</param>
    /// <param name="exception">Exception that caused failure (if any)</param>
    void LogEmailSendFailure(string correlationId, string templateName, string errorMessage, Exception? exception = null);

    /// <summary>
    /// Logs parameter validation failures.
    /// Called when IEmailParameters.Validate() returns false.
    /// CRITICAL: This prevents the silent failures seen in Phase 6A (literal {{Parameters}} in emails)
    /// </summary>
    /// <param name="correlationId">Correlation ID from start</param>
    /// <param name="templateName">Email template with invalid parameters</param>
    /// <param name="errors">List of validation error messages</param>
    void LogParameterValidationFailure(string correlationId, string templateName, List<string> errors);

    /// <summary>
    /// Logs when email template not found in database.
    /// CRITICAL: This alerts to the kind of issue found by other agent (template name mismatch)
    /// </summary>
    /// <param name="correlationId">Correlation ID from start</param>
    /// <param name="templateName">Template name that was not found</param>
    void LogTemplateNotFound(string correlationId, string templateName);

    /// <summary>
    /// Logs feature flag checks for hybrid system rollout monitoring.
    /// Tracks which handlers are using typed vs Dictionary parameters.
    /// </summary>
    /// <param name="handlerName">Handler class name</param>
    /// <param name="isEnabled">Whether typed parameters enabled for this handler</param>
    /// <param name="reason">Reason (global setting, override, etc.)</param>
    void LogFeatureFlagCheck(string handlerName, bool isEnabled, string reason);

    /// <summary>
    /// Generates a new correlation ID for email send operation.
    /// Returns GUID string for uniqueness and consistency.
    /// </summary>
    /// <returns>New correlation ID</returns>
    string GenerateCorrelationId();
}
