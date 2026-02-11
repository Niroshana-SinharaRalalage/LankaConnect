using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ReopenEventForm;

/// <summary>
/// Reopens a closed event form (Closed -> Active).
/// </summary>
public record ReopenEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;
