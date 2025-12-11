using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelEvent;

public record CancelEventCommand(
    Guid EventId,
    string CancellationReason
) : ICommand;
