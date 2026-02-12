using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateEventForm;

/// <summary>
/// Updates event form details (title, description, settings).
/// Does not modify questions - use question-specific commands for that.
/// </summary>
public record UpdateEventFormCommand(
    Guid EventId,
    Guid FormId,
    string Title,
    string? Description,
    bool AllowMultipleResponses,
    DateTime? ResponseDeadline,
    int? MaxResponses
) : ICommand;
