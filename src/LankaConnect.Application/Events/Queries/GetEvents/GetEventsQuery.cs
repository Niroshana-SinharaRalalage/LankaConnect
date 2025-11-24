using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetEvents;

/// <summary>
/// Query to get events with optional filtering and location-based sorting
/// Location-based sorting (when UserId or Lat/Lng provided):
/// - For authenticated users with preferred metro areas: Sort by those metros
/// - For authenticated users without preferences: Sort by user's location
/// - For anonymous users: Sort by provided coordinates
/// </summary>
public record GetEventsQuery(
    EventStatus? Status = null,
    EventCategory? Category = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    bool? IsFreeOnly = null,
    string? City = null,
    string? State = null,
    Guid? UserId = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    List<Guid>? MetroAreaIds = null
) : IQuery<IReadOnlyList<EventDto>>;
