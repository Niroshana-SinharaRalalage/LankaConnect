using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.RsvpToEvent;

namespace LankaConnect.Application.Events.Commands.UpdateRegistrationDetails;

/// <summary>
/// Phase 6A.14: Command to update registration details (attendees and contact information)
/// Allows users to edit their registration after initial RSVP
/// </summary>
public record UpdateRegistrationDetailsCommand(
    Guid EventId,
    Guid UserId,
    List<AttendeeDto> Attendees,
    string Email,
    string PhoneNumber,
    string? Address = null
) : ICommand;
