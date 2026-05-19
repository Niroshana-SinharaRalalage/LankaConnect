using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.RsvpToEvent;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;

/// <summary>
/// Session 21: Updated to support multiple attendees with individual names and age categories
/// Legacy format still supported for backward compatibility (single attendee with Name/Age)
/// New format: List of AttendeeDto objects with AgeCategory and Gender
/// </summary>
/// <summary>
/// Phase 6A.44: Updated to return Result<string?> for Stripe checkout URL
/// - Returns null for FREE events (registration completes immediately)
/// - Returns checkout URL for PAID events (user must complete payment)
/// Added SuccessUrl and CancelUrl for Stripe Checkout redirect
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
    int Quantity = 1,
    // Phase 6A.44: Stripe checkout URLs (required for paid events)
    string? SuccessUrl = null,
    string? CancelUrl = null,
    // Donation Feature: Optional donation during registration
    // C3 Guard: Always check > 0, not just HasValue. Treat 0 same as null.
    decimal? DonationAmount = null,
    string? DonorName = null,
    string? DonorPhone = null,
    string? DonorNotes = null,
    // Phase 6A.137F: Add-on, collection, and sponsor fields for bundled checkout
    List<AddOnSelectionDto>? AddOnSelections = null,
    decimal? CollectionAmount = null,
    string? CollectionNotes = null,
    decimal? SponsorAmount = null,
    string? SponsorOrganization = null,
    string? SponsorNotes = null,
    // Phase 6A.151 C5 — optional sponsor logo from POST /sponsors/staging-image.
    // Anonymous registration flow: same semantics as RsvpToEventCommand. Both
    // fields supplied together or both null. Handler calls Sponsor.SetImage
    // in-tx with the Sponsor row create.
    string? SponsorStagingBlobName = null,
    string? SponsorStagingBlobUrl = null,
    // Phase 7A.6D: WhatsApp opt-in during registration
    string? WhatsAppPhoneNumber = null,
    // Phase 7E.3a: Head-count payload for B-mode events. Mutually exclusive with Attendees;
    // the handler dispatches by event.RegistrationMode. Reuses RsvpToEvent.HeadCountDto and
    // TierCountDto via the existing using statement.
    string? LeadAttendeeName = null,
    HeadCountDto? HeadCount = null,
    // Phase 8 S8.2.B: Assigned-seating fields. Same semantics as
    // RsvpToEventCommand.SeatIds/SeatSessionId — required when the event's
    // SeatingMode == AssignedSeating, rejected on GeneralAdmission events.
    List<Guid>? SeatIds = null,
    string? SeatSessionId = null
) : ICommand<string?>;

/// <summary>
/// Individual attendee information with age category and optional gender.
/// Phase 8 S8.2.D: Optional ticket tier assignment so anonymous buyers can
/// register for tiered events (mirrors RsvpToEvent.AttendeeDto).
/// </summary>
public record AttendeeDto(
    string Name,
    AgeCategory AgeCategory,
    Gender? Gender = null,
    Guid? TicketTierId = null
);
