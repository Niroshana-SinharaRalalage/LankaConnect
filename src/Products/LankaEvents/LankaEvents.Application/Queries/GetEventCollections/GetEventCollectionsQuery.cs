using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventCollections;

/// <summary>
/// Query to get all collections (event fund contributions) for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventCollectionsQuery(Guid EventId) : IQuery<EventCollectionsResponse>;
