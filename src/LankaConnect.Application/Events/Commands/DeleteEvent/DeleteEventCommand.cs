using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.DeleteEvent;

public record DeleteEventCommand(Guid EventId, Guid UserId) : ICommand;
