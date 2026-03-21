using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventCollections;

/// <summary>
/// Query to get all collections (event fund contributions) for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventCollectionsQuery(Guid EventId) : IQuery<EventCollectionsResponse>;
