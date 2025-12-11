using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;

/// <summary>
/// Query to get user's registration details for a specific event
/// Returns full registration including attendee names and ages
/// </summary>
public record GetUserRegistrationForEventQuery(
    Guid EventId,
    Guid UserId
) : IQuery<RegistrationDetailsDto?>;
