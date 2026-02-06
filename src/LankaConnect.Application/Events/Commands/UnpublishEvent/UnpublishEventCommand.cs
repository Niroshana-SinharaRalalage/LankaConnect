using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UnpublishEvent;

/// <summary>
/// Phase 6A.41: Command to unpublish a published event, returning it to Draft status.
/// </summary>
public record UnpublishEventCommand(Guid EventId) : ICommand;
