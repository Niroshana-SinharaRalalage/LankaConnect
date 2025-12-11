using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetMyRegisteredEvents;

/// <summary>
/// Query to get events that the current user has registered for
/// Epic 1: Returns full EventDto instead of RsvpDto for better dashboard UX
/// </summary>
public record GetMyRegisteredEventsQuery(Guid UserId) : IQuery<IReadOnlyList<EventDto>>;
