namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module read-only projection of a refund request WITHOUT its line items.
/// Returned by the list-shaped <see cref="IPaymentQueries"/> methods.
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). Field surface tracks the
/// <c>LankaConnect.Domain.Events.Entities.RefundRequest</c> public state as of
/// Phase 6A.148. <see cref="LineItemCount"/> is provided for list-page rendering
/// without forcing a detail fetch; callers needing the full line-item array
/// request <see cref="RefundRequestDetailDto"/> via
/// <see cref="IPaymentQueries.GetRefundRequestWithLineItemsAsync"/>.
/// <para>
/// <see cref="IsActive"/> mirrors the aggregate-internal predicate used by the
/// "single active refund per registration" invariant at
/// <c>Registration.cs:1073</c> - kept on this DTO so cross-module read callers
/// (e.g. EventCancellationEmailJob) can answer the same question without
/// re-implementing the state-set check.
/// </para>
/// </remarks>
public sealed record RefundRequestSummaryDto(
    Guid Id,
    Guid RegistrationId,
    Guid RequestedByUserId,
    bool IsOrganizerInitiated,
    DateTime RequestedAt,
    RefundRequestStatusDto Status,
    string? RequesterReason,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? OrganizerNotes,
    string? RejectionReason,
    DateTime? CompletedAt,
    int LineItemCount,
    bool IsActive);
