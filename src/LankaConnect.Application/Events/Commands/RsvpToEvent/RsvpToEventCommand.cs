using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

/// <summary>
/// Session 21: Updated to support multiple attendees for authenticated users
/// Session 23: Returns Stripe Checkout session URL for paid events (null for free events)
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
    string? Address = null,
    // Session 23: Payment integration - URLs for Stripe Checkout redirect
    string? SuccessUrl = null,
    string? CancelUrl = null
) : ICommand<string?>;  // Returns checkout session URL for paid events, null for free events

/// <summary>
/// Individual attendee information with age category and optional gender
/// </summary>
public record AttendeeDto(
    string Name,
    AgeCategory AgeCategory,
    Gender? Gender = null
);
