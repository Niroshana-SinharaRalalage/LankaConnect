using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.PublishEventForm;

/// <summary>
/// Publishes an event form (Draft -> Active).
/// Form must have at least one question to be published.
/// </summary>
public record PublishEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;
