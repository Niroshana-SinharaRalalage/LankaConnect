using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventDonations;

/// <summary>
/// Query to get all donations for an event with summary statistics.
/// Organizer-only access.
/// </summary>
public record GetEventDonationsQuery(Guid EventId) : IQuery<EventDonationsResponse>;
