namespace LankaConnect.Modules.Communications.Contracts.Email.Services;

/// <summary>
/// Phase 6A.148.W5.6.B.OBS3 — writes durable suppression rows to
/// <c>communications.email_dispatch_log</c> from places that DELIBERATELY do NOT
/// send an email (e.g., the D9 workflow-owned-refund guard in SponsorWebhookHandler /
/// CollectionWebhookHandler).
///
/// 4C.d.xiv (2026-07-08): moved from Communications.Infrastructure to Contracts per
/// Consult #15 PASS C (interfaces + DTO signatures live in Module.Contracts).
/// Enables Payments.Infrastructure to consume without a PR to Communications.Infrastructure.
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
