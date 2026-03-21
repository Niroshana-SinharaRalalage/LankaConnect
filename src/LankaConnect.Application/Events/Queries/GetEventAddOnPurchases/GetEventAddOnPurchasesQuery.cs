using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventAddOnPurchases;

/// <summary>
/// Query to get all add-on definitions and purchases for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventAddOnPurchasesQuery(Guid EventId) : IQuery<EventAddOnPurchasesResponse>;
