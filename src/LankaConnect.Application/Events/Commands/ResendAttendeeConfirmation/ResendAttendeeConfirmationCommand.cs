using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.ResendAttendeeConfirmation;

/// <summary>
/// Phase 6A.X: Command to resend registration confirmation email to a specific attendee (Organizer action).
/// Allows organizers to manually resend confirmation emails from the Attendees tab.
/// Works for both free and paid event registrations via shared email service.
/// </summary>
/// <param name="EventId">ID of the event</param>
/// <param name="RegistrationId">ID of the registration</param>
/// <param name="OrganizerId">ID of the organizer (for authorization validation)</param>
public record ResendAttendeeConfirmationCommand(
    Guid EventId,
    Guid RegistrationId,
    Guid OrganizerId
) : ICommand;
