using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Application.Events.Queries.GetEventsByOrganizer;

/// <summary>
/// Query to get events created by a specific organizer
/// Phase 6A.47: Added filter parameters (SearchTerm, Category, Date Range, Location)
/// Issue #36: Added StatusFilter for user-friendly status filtering
/// </summary>
public record GetEventsByOrganizerQuery(
    Guid OrganizerId,
    string? SearchTerm = null,
    EventCategory? Category = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    string? State = null,
    List<Guid>? MetroAreaIds = null,
    /// <summary>
    /// Issue #36: User-friendly status filter (Active, Inactive, Cancelled, Unpublished, All)
    /// </summary>
    EventStatusFilter? StatusFilter = null
) : IQuery<IReadOnlyList<EventDto>>;
