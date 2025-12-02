using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;

/// <summary>
/// Session 21: Updated to support multiple attendees with individual names and ages
/// Legacy format still supported for backward compatibility (single attendee with Name/Age)
/// New format: List of AttendeeDto objects
/// </summary>
public record RegisterAnonymousAttendeeCommand(
    Guid EventId,
    // Legacy format (Session 20 - backward compatibility)
    string? Name,
    int? Age,
    // New format (Session 21 - multi-attendee)
    List<AttendeeDto>? Attendees,
    // Contact information (shared for all attendees)
    string Email,
    string PhoneNumber,
    string? Address,
    // Legacy quantity field (backward compatibility)
    int Quantity = 1
) : ICommand;

/// <summary>
/// Session 21: Individual attendee information
/// </summary>
public record AttendeeDto(
    string Name,
    int Age
);
