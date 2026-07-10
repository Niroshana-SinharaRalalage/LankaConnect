namespace LankaConnect.Modules.Communications.Contracts.Email.Contracts;

/// <summary>
/// Phase 6A.148.W5.6.B.OBS2 — optional marker interface for email parameter
/// contracts that should produce a row in <c>communications.email_dispatch_log</c>
/// when dispatched. Implemented by every refund-flow email params class so the
/// operator post-mortem flow can answer "what did this refund send?" via a
/// single indexed SQL query, without depending on container-stdout retention.
///
/// Non-refund emails (event reminders, sponsor onboarding, welcome, etc.) do NOT
/// need to implement this — the dispatch log is intentionally scoped to refund
/// flow to keep the table from becoming a generic email firehose. Adding more
/// surface area later is opt-in via this interface.
/// </summary>
public interface IDispatchLoggable
{
    /// <summary>
    /// The RefundRequest aggregate this email is dispatched on behalf of.
    /// Indexed in dispatch log; powers "what did this RR send?" query.
    /// </summary>
    Guid? DispatchRefundRequestId { get; }

    /// <summary>
    /// Optional discriminator for non-refund-request-scoped emails (e.g., a
    /// per-sponsor or per-collection refund notification). Combined with
    /// <see cref="DispatchEntityId"/> as a filtered index on the log table.
    /// Examples: "Sponsor", "Collection", "AddOnPurchase".
    /// </summary>
    string? DispatchEntityType { get; }

    /// <summary>
    /// Optional entity id paired with <see cref="DispatchEntityType"/>.
    /// </summary>
    Guid? DispatchEntityId { get; }
}
