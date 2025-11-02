using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

public record CancelRsvpCommand(Guid EventId, Guid UserId) : ICommand;
