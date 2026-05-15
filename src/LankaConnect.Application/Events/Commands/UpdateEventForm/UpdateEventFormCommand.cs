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
    int? MaxResponses,
    // Phase 6A.146: nullable so legacy callers (which don't supply this) pass null
    // and EventForm.UpdateDetails(...) leaves the flag unchanged. UI sends the
    // user's intent explicitly (true/false).
    bool? AllowAttendeesToViewResponses = null
) : ICommand;
