using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CloseEventForm;

/// <summary>
/// Closes an event form (Active -> Closed). No more responses accepted.
/// </summary>
public record CloseEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;
