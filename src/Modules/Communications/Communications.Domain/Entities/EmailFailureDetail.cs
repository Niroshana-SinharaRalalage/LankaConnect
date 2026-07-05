using LankaConnect.Domain.Common;
namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Phase 6A.99: Domain entity for persisting email failure details to the database.
///
/// Problem Solved:
/// - Email failure details were stored only in memory (ConcurrentBag)
/// - Azure Container Apps restarts caused complete loss of failure details
/// - Dashboard showed "Historical failure details unavailable" after restart
/// - Admin users could not debug email issues after container restart
///
/// Solution:
/// - Persist failure details to database with 30-day retention
/// - Store encrypted email addresses for GDPR compliance
/// - Load historical failures on startup
/// - Auto-cleanup expired records via background service
/// </summary>
public class EmailFailureDetail : LegacyBaseEntity
{
    /// <summary>
    /// When the email failure occurred
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Name of the email template that failed
    /// </summary>
    public string TemplateName { get; private set; } = string.Empty;

    /// <summary>
    /// Encrypted recipient email address (GDPR compliant)
    /// </summary>
    public string RecipientEmailEncrypted { get; private set; } = string.Empty;

    /// <summary>
    /// Full error message for debugging
    /// </summary>
    public string ErrorMessage { get; private set; } = string.Empty;

    /// <summary>
    /// Name of the handler/service that attempted to send the email
    /// </summary>
    public string? HandlerName { get; private set; }

    /// <summary>
    /// Correlation ID for request tracing
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// When this record expires (for 30-day retention policy)
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    // For EF Core
    private EmailFailureDetail() { }

    private EmailFailureDetail(
        string correlationId,
        string templateName,
        string recipientEmailEncrypted,
        string errorMessage,
        string? handlerName,
        int retentionDays)
    {
        Timestamp = DateTime.UtcNow;
        CorrelationId = correlationId;
        TemplateName = templateName;
        RecipientEmailEncrypted = recipientEmailEncrypted;
        ErrorMessage = errorMessage;
        HandlerName = handlerName;
        ExpiresAt = DateTime.UtcNow.AddDays(retentionDays);
    }

    /// <summary>
    /// Creates a new email failure detail record
    /// </summary>
    /// <param name="correlationId">Request correlation ID for tracing</param>
    /// <param name="templateName">Name of the email template</param>
    /// <param name="recipientEmailEncrypted">Encrypted recipient email (use IEmailEncryptionService)</param>
    /// <param name="errorMessage">Full error message</param>
    /// <param name="handlerName">Handler/service name that sent the email</param>
    /// <param name="retentionDays">Days to retain this record (default: 30)</param>
    public static EmailFailureDetail Create(
        string correlationId,
        string templateName,
        string recipientEmailEncrypted,
        string errorMessage,
        string? handlerName = null,
        int retentionDays = 30)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("Error message is required", nameof(errorMessage));

        return new EmailFailureDetail(
            correlationId ?? string.Empty,
            templateName,
            recipientEmailEncrypted ?? string.Empty,
            errorMessage,
            handlerName,
            retentionDays);
    }

    /// <summary>
    /// Checks if this record has expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}