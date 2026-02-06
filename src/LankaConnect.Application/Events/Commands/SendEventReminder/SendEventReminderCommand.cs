using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Events.Commands.SendEventReminder;

/// <summary>
/// Phase 6A.76: Command to send reminder email to all registered attendees of an event.
/// Allows organizers to trigger reminders at any time from the Communications tab.
/// </summary>
public record SendEventReminderCommand(
    Guid EventId,
    string ReminderType = "1day" // "1day", "2day", "7day", or "custom"
) : IRequest<Result<int>>;
