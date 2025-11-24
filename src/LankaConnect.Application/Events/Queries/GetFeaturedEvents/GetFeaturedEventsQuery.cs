using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetFeaturedEvents;

/// <summary>
/// Query to get featured events for the landing page
/// Returns 4 events sorted by location relevance:
/// - For authenticated users with preferred metro areas: Sort by those metros
/// - For authenticated users without preferences: Sort by user's location
/// - For anonymous users: Sort by provided coordinates
/// </summary>
public record GetFeaturedEventsQuery(
    Guid? UserId = null,
    decimal? Latitude = null,
    decimal? Longitude = null
) : IQuery<IReadOnlyList<EventDto>>;
