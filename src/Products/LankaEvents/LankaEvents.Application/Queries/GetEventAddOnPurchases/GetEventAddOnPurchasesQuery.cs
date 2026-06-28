using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventAddOnPurchases;

/// <summary>
/// Query to get all add-on definitions and purchases for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventAddOnPurchasesQuery(Guid EventId) : IQuery<EventAddOnPurchasesResponse>;
