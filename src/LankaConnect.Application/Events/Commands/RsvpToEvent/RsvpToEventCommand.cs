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
    string? CancelUrl = null,
    // Donation Feature: Optional donation during registration
    // C3 Guard: Always check > 0, not just HasValue. Treat 0 same as null.
    decimal? DonationAmount = null,
    string? DonorName = null,
    string? DonorPhone = null,
    string? DonorNotes = null,
    // Phase 6A.137D: Optional add-ons bundled with registration checkout
    // C2 Guard: Add-on failures are isolated — registration succeeds even if add-on creation fails.
    List<AddOnSelectionDto>? AddOnSelections = null,
    // Phase 6A.137E: Optional collection (event fund) contribution during registration
    decimal? CollectionAmount = null,
    string? CollectionNotes = null,
    // Phase 6A.137E: Optional money sponsorship during registration
    decimal? SponsorAmount = null,
    string? SponsorOrganization = null,
    string? SponsorNotes = null
) : ICommand<string?>;  // Returns checkout session URL for paid events, null for free events

/// <summary>
/// Individual attendee information with age category and optional gender
/// </summary>
public record AttendeeDto(
    string Name,
    AgeCategory AgeCategory,
    Gender? Gender = null
);

/// <summary>
/// Phase 6A.137D: Add-on selection during registration.
/// DefinitionId references the AddOnDefinition; Quantity is the number of units.
/// </summary>
public record AddOnSelectionDto(
    Guid DefinitionId,
    int Quantity
);
