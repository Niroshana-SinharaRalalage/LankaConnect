namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module read-only projection of a refund request WITH its line items.
/// Returned by <see cref="IPaymentQueries.GetRefundRequestWithLineItemsAsync"/>.
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). Same field surface as
/// <see cref="RefundRequestSummaryDto"/> with the line items array hydrated.
/// </remarks>
public sealed record RefundRequestDetailDto(
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
    bool IsActive,
    IReadOnlyList<RefundLineItemDto> LineItems)
{
    public int LineItemCount => LineItems.Count;
}
