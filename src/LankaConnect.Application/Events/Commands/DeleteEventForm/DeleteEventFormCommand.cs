using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.DeleteEventForm;

/// <summary>
/// Deletes an event form. Only allowed if the form has no responses.
/// </summary>
public record DeleteEventFormCommand(
    Guid EventId,
    Guid FormId
) : ICommand;
