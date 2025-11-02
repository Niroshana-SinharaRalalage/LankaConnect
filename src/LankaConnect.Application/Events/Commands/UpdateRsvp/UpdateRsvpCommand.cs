using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateRsvp;

public record UpdateRsvpCommand(Guid EventId, Guid UserId, int NewQuantity) : ICommand;
