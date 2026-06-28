using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CancelEvent;

public record CancelEventCommand(
    Guid EventId,
    string CancellationReason
) : ICommand;
