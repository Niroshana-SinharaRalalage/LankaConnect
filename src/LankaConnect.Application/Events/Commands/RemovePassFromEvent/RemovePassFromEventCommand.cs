using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RemovePassFromEvent;

public record RemovePassFromEventCommand(
    Guid EventId,
    Guid PassId
) : ICommand;
