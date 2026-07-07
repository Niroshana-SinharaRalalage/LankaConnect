using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Identity;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Modules.Payments.Application.Queries.RefundRequests;

/// <summary>
/// Phase 6A.148: Organizer queue listing for an event. Filterable by status; returns
/// organizer-facing DTO (includes <c>OrganizerNotes</c>, requester identity, etc.).
/// Caller must be an organizer of the event (handler verifies via Event.IsOrganizer).
/// </summary>
public record GetEventRefundRequestsQuery(
    Guid EventId,
    Guid CallerUserId,
    RefundRequestStatus? StatusFilter
) : IQuery<IReadOnlyList<OrganizerRefundRequestDto>>;
