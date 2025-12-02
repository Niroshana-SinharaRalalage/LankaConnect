using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

/// <summary>
/// Session 21: Updated to support multiple attendees for authenticated users
/// Legacy format: Quantity (number of attendees without details)
/// New format: List of AttendeeDto with contact information
/// </summary>
public record RsvpToEventCommand(
    Guid EventId,
    Guid UserId,
    // Legacy format (backward compatibility)
    int Quantity = 1,
    // New format (Session 21 - multi-attendee)
    List<AttendeeDto>? Attendees = null,
    // Contact information (new format only)
    string? Email = null,
    string? PhoneNumber = null,
    string? Address = null
) : ICommand;

/// <summary>
/// Session 21: Individual attendee information
/// </summary>
public record AttendeeDto(
    string Name,
    int Age
);
