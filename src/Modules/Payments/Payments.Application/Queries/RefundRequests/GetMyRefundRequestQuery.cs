using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Identity;
namespace LankaConnect.Modules.Payments.Application.Queries.RefundRequests;

/// <summary>
/// Phase 6A.148: Attendee fetches their own most-recent refund request for an event,
/// or null if none exists. Returns an <see cref="AttendeeRefundRequestDto"/> which
/// excludes internal-only <c>OrganizerNotes</c> (architect F6).
/// </summary>
public record GetMyRefundRequestQuery(
    Guid EventId,
    Guid CallerUserId
) : IQuery<AttendeeRefundRequestDto?>;
