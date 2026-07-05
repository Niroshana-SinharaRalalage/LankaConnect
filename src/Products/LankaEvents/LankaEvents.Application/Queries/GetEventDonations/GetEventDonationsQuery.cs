using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventDonations;

/// <summary>
/// Query to get all donations for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventDonationsQuery(Guid EventId) : IQuery<EventDonationsResponse>;
