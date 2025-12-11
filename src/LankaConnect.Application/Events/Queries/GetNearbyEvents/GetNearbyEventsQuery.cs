using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetNearbyEvents;

/// <summary>
/// Query to get events within a specified radius of a location (Epic 2 Phase 3 - Spatial Queries)
/// Uses PostGIS spatial queries via EventRepository
/// </summary>
public record GetNearbyEventsQuery(
    decimal Latitude,
    decimal Longitude,
    double RadiusKm,
    EventCategory? Category = null,
    bool? IsFreeOnly = null,
    DateTime? StartDateFrom = null
) : IQuery<IReadOnlyList<EventDto>>;
