using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Entities;

/// <summary>
/// Phase 6A.148.W5.6.B.OBS — durable audit row for every transactional email dispatched
/// (or suppressed) by the application. Closes the operator-UAT observability gap where
/// container stdout retention (~25 min) and a perpetually-empty <c>email_messages</c>
/// table left every refund-flow complaint requiring screenshot forensics.
///
/// Written SYNCHRONOUSLY by <c>ITypedEmailService.SendEmailAsync</c> immediately before
/// (or in place of, when suppressed) the SendGrid/Azure Email handoff. Recorded as
/// best-effort: log-write failures MUST NOT block the actual email send. The two
/// operations are sequenced (log first, send second) so that a successful send always
/// has a row; a failed log → failed send sequence is acceptable degradation (we lose
/// the row but the email still goes out, and Serilog still captures the send-failure).
///
/// Use cases (queryable via simple SQL):
/// - "What did this RR send?" → <c>WHERE refund_request_id = 'X'</c> shows every email
///   recipient + template + suppressed-or-sent + reason
/// - "Was D9 suppression invoked for sponsor X?" → <c>WHERE entity_id = '&lt;sponsor-id&gt;'
///   AND suppressed = true</c>
/// - "What emails went to recipient X today?" →
///   <c>WHERE recipient_email = 'X' AND dispatched_at &gt;= now() - interval '1 day'</c>
/// </summary>
public class EmailDispatchLog : BaseEntity
{
    public Guid CorrelationId { get; private set; }
    public Guid? RefundRequestId { get; private set; }
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string TemplateName { get; private set; } = string.Empty;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string? RecipientName { get; private set; }
    public string? SubjectRendered { get; private set; }
    public string? PayloadJson { get; private set; }
    public bool Suppressed { get; private set; }
    public string? SuppressionReason { get; private set; }
    public DateTime DispatchedAt { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ProviderStatus { get; private set; }

    /// <summary>EF Core constructor.</summary>
    private EmailDispatchLog() { }

    /// <summary>
    /// Factory for a SENT row (email actually handed to the provider).
    /// </summary>
    public static EmailDispatchLog ForSend(
        Guid correlationId,
        string templateName,
        string recipientEmail,
        string? recipientName,
        string? subjectRendered,
        string? payloadJson,
        Guid? refundRequestId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? providerMessageId = null,
        string? providerStatus = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("templateName required", nameof(templateName));
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("recipientEmail required", nameof(recipientEmail));

        var now = DateTime.UtcNow;
        return new EmailDispatchLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            RefundRequestId = refundRequestId,
            EntityType = entityType,
            EntityId = entityId,
            TemplateName = templateName,
            RecipientEmail = recipientEmail.Trim(),
            RecipientName = recipientName?.Trim(),
            SubjectRendered = subjectRendered,
            PayloadJson = payloadJson,
            Suppressed = false,
            SuppressionReason = null,
            DispatchedAt = now,
            ProviderMessageId = providerMessageId,
            ProviderStatus = providerStatus,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Factory for a SUPPRESSED row (caller deliberately did NOT send — e.g., D9
    /// workflow-owned-refund guard in SponsorWebhookHandler).
    /// </summary>
    public static EmailDispatchLog ForSuppress(
        Guid correlationId,
        string templateName,
        string recipientEmail,
        string? recipientName,
        string suppressionReason,
        Guid? refundRequestId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? payloadJson = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("templateName required", nameof(templateName));
        if (string.IsNullOrWhiteSpace(recipientEmail))
            throw new ArgumentException("recipientEmail required", nameof(recipientEmail));
        if (string.IsNullOrWhiteSpace(suppressionReason))
            throw new ArgumentException("suppressionReason required when Suppressed=true", nameof(suppressionReason));

        var now = DateTime.UtcNow;
        return new EmailDispatchLog
        {
            Id = Guid.NewGuid(),
            CorrelationId = correlationId,
            RefundRequestId = refundRequestId,
            EntityType = entityType,
            EntityId = entityId,
            TemplateName = templateName,
            RecipientEmail = recipientEmail.Trim(),
            RecipientName = recipientName?.Trim(),
            SubjectRendered = null,
            PayloadJson = payloadJson,
            Suppressed = true,
            SuppressionReason = suppressionReason,
            DispatchedAt = now,
            ProviderMessageId = null,
            ProviderStatus = "suppressed",
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Update the provider message id / status after the actual send completes.
    /// Idempotent on already-populated rows; overwrites any prior value.
    /// </summary>
    public void RecordProviderResponse(string? providerMessageId, string? providerStatus)
    {
        ProviderMessageId = providerMessageId;
        ProviderStatus = providerStatus;
        MarkAsUpdated();
    }
}
