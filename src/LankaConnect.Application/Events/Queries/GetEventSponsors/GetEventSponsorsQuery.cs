using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventSponsors;

/// <summary>
/// Query to get all sponsors for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventSponsorsQuery(Guid EventId) : IQuery<EventSponsorsResponse>;
