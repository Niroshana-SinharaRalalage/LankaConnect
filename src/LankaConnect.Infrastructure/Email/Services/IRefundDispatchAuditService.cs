namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.148.W5.6.B.OBS3 — writes durable suppression rows to
/// <c>communications.email_dispatch_log</c> from places that DELIBERATELY do NOT
/// send an email (e.g., the D9 workflow-owned-refund guard in SponsorWebhookHandler /
/// CollectionWebhookHandler).
///
/// Without this, operators cannot tell "we never sent" from "we tried and the provider
/// rejected" — both produce empty SendGrid inboxes. The dispatch-log row captures
/// intent.
///
/// Best-effort by contract — implementations MUST swallow persist exceptions so the
/// audit write never blocks the caller's primary control flow.
/// </summary>
public interface IRefundDispatchAuditService
{
    Task WriteSuppressionAsync(
        string templateName,
        string recipientEmail,
        string? recipientName,
        string suppressionReason,
        Guid correlationId,
        Guid? refundRequestId,
        string? entityType,
        Guid? entityId,
        CancellationToken cancellationToken = default);
}
