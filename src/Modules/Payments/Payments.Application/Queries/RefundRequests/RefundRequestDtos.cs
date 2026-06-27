using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Modules.Payments.Application.Queries.RefundRequests;

/// <summary>
/// Phase 6A.148: Attendee-facing refund request projection.
/// CRITICAL (architect F6): <c>OrganizerNotes</c> is intentionally excluded — it's an
/// internal audit field and must not leak to the requesting attendee. <c>RejectionReason</c>
/// is the customer-facing field shown in the rejection email and UI banner.
/// </summary>
public record AttendeeRefundRequestDto(
    Guid Id,
    Guid RegistrationId,
    RefundRequestStatus Status,
    DateTime RequestedAt,
    string? RequesterReason,
    DateTime? ReviewedAt,
    string? RejectionReason,
    DateTime? CompletedAt,
    IReadOnlyList<RefundLineItemDto> LineItems);

/// <summary>
/// Phase 6A.148: Organizer-facing refund request projection.
/// Includes <c>OrganizerNotes</c>, requester identity, and scan-guard-override audit fields.
/// </summary>
public record OrganizerRefundRequestDto(
    Guid Id,
    Guid RegistrationId,
    Guid RequestedByUserId,
    bool IsOrganizerInitiated,
    RefundRequestStatus Status,
    DateTime RequestedAt,
    string? RequesterReason,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? OrganizerNotes,
    string? RejectionReason,
    DateTime? CompletedAt,
    bool ScanGuardOverridden,
    IReadOnlyList<RefundLineItemDto> LineItems);

public record RefundLineItemDto(
    Guid Id,
    RefundLineItemType Type,
    Guid ReferenceId,
    decimal RequestedAmount,
    Currency RequestedCurrency,
    decimal? ApprovedAmount,
    Currency? ApprovedCurrency,
    RefundLineItemStatus Status,
    string? StripeRefundId,
    DateTime? ProcessedAt,
    string? FailureReason);
