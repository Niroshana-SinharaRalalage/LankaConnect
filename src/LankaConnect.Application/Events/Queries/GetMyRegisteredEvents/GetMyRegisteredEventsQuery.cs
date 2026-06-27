using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetMyRegisteredEvents;

/// <summary>
/// Query to get events that the current user has registered for
/// Epic 1: Returns full EventDto instead of RsvpDto for better dashboard UX
/// Phase 6A.47: Added filter parameters (SearchTerm, Category, Date Range, Location)
/// </summary>
public record GetMyRegisteredEventsQuery(
    Guid UserId,
    string? SearchTerm = null,
    EventCategory? Category = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    string? State = null,
    List<Guid>? MetroAreaIds = null
) : IQuery<IReadOnlyList<EventDto>>;
