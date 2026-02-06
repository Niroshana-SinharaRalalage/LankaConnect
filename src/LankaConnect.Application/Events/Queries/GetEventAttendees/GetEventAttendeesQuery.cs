using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventAttendees;

/// <summary>
/// Query to retrieve all attendees for an event (organizer only).
/// </summary>
public record GetEventAttendeesQuery(Guid EventId) : IQuery<EventAttendeesResponse>;
