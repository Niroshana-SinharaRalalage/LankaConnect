using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;

namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEventAttendees;

/// <summary>
/// Query to retrieve all attendees for an event (organizer only).
/// </summary>
public record GetEventAttendeesQuery(Guid EventId) : IQuery<EventAttendeesResponse>;
