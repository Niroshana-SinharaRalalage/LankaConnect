using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetEventsByOrganizer;

public record GetEventsByOrganizerQuery(Guid OrganizerId) : IQuery<IReadOnlyList<EventDto>>;
