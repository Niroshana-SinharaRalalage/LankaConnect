using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent;

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
    string? SponsorNotes = null,
    // Phase 6A.148.W5.D10.c — sponsor contact-detail parity with the standalone
    // /events/{id}/sponsors form. When the bundled-at-registration flow sends these,
    // they OVERRIDE the registering user's defaults (so the sponsor row can attribute
    // to a colleague / company representative whose details differ from the
    // checkout user). All three are optional — when blank, the handler falls back
    // to Attendees[0].Name + Email + PhoneNumber as before.
    string? SponsorName = null,
    string? SponsorEmail = null,
    string? SponsorPhone = null,
    // Phase 6A.151 C5: Optional sponsor logo/image staging-blob from POST /sponsors/staging-image.
    // FE pre-uploads the image when the user picks it (since the inline panel
    // submits via the parent registration command which then creates the
    // Stripe Checkout session server-side — there is no Sponsor row to attach
    // the blob to pre-Stripe). The handler reads these and calls
    // Sponsor.SetImage(blobUrl, blobName) in-tx with the Sponsor row create.
    // Both must be supplied together or both null.
    string? SponsorStagingBlobName = null,
    string? SponsorStagingBlobUrl = null,
    // Phase 7A.6D: WhatsApp opt-in during registration
    string? WhatsAppPhoneNumber = null,
    // Phase 7E.3a: Head-count RSVP fields for B-mode events. Mutually exclusive with Attendees.
    // The handler chooses between Attendees (Mode A) and HeadCount (Mode B) based on the
    // event's RegistrationMode — sending the wrong shape returns 400 with a clear message.
    // LeadAttendeeName is the named "lead" attendee (printed in emails); HeadCount carries
    // the multi-axis breakdown (Total + Demographics? + TierCounts?).
    string? LeadAttendeeName = null,
    HeadCountDto? HeadCount = null,
    // Phase 8 S8.2.B: Assigned-seating fields. Required when the event's
    // SeatingMode == AssignedSeating (one seat per attendee, all seats
    // currently held in SeatSessionId, none already reserved). Rejected
    // with 400 if either field is set on a GeneralAdmission event.
    List<Guid>? SeatIds = null,
    string? SeatSessionId = null
) : ICommand<string?>;  // Returns checkout session URL for paid events, null for free events

/// <summary>
/// Individual attendee information with age category and optional gender
/// </summary>
public record AttendeeDto(
    string Name,
    AgeCategory AgeCategory,
    Gender? Gender = null,
    // Phase 8: Optional ticket tier assignment for tiered events
    Guid? TicketTierId = null
);

/// <summary>
/// Phase 6A.137D: Add-on selection during registration.
/// DefinitionId references the AddOnDefinition; Quantity is the number of units.
/// </summary>
public record AddOnSelectionDto(
    Guid DefinitionId,
    int Quantity
);

/// <summary>
/// Phase 7E.3a: Head-count payload for B-mode RSVPs. Mode-specific factories on
/// <see cref="LankaConnect.Products.LankaEvents.Domain.ValueObjects.HeadCountBreakdown"/> validate which fields
/// are required; this DTO accepts everything optional so a single DTO can be sent regardless of mode.
///
/// Field requirements by event mode:
/// - HeadCountOnly (B1): <see cref="Total"/> required.
/// - HeadCountByAge (B2): <see cref="Adults"/> + <see cref="Children"/> required (Total auto-derived).
/// - HeadCountByGender (B3): <see cref="Males"/> + <see cref="Females"/> required (Total auto-derived).
/// - HeadCountByAgeAndGender (B4): four leaf counts required (Total auto-derived).
/// - <see cref="TierCounts"/> required iff the event has ticket tiers configured (7E.3c).
/// </summary>
public record HeadCountDto(
    int? Total = null,
    int? Adults = null,
    int? Children = null,
    int? Males = null,
    int? Females = null,
    int? AdultMales = null,
    int? AdultFemales = null,
    int? ChildMales = null,
    int? ChildFemales = null,
    List<TierCountDto>? TierCounts = null
);

/// <summary>
/// Phase 7E.3c: Per-tier count for a registration. <c>TierName</c> is resolved server-side
/// from <c>EventId + TierId</c> at command-handle time and snapshotted onto the registration —
/// the client supplies <c>TierId</c> + <c>Count</c> only.
///
/// Phase 7F-C (architect-approved 2026-04-30): optional <c>AdultCount</c> + <c>ChildCount</c>
/// per-tier-by-age axis. Used in B2 / B4 modes with tiered pricing when the organiser opts
/// into per-tier-by-age billing (adults pay <c>tier.AdultPrice</c>, children pay
/// <c>tier.ChildPrice</c>). Domain invariant: both fields set or both null (half-set is
/// rejected by <c>TierCount.Create</c>); when set, sum must equal <c>Count</c>.
/// </summary>
public record TierCountDto(
    Guid TierId,
    int Count,
    int? AdultCount = null,
    int? ChildCount = null,
    // Phase 7F-E.7 (architect-approved 2026-05-04, re-opens §2.2 #4 deferred decision):
    // optional per-tier 4-leaf demographic split for B4-mode + tiered registrations.
    // All-or-nothing per tier; sum equals Count. Domain factory enforces both invariants.
    // When set, age split (AdultCount/ChildCount) is auto-derived for back-compat with
    // the 7F-C pricing helper.
    int? AdultMaleCount = null,
    int? AdultFemaleCount = null,
    int? ChildMaleCount = null,
    int? ChildFemaleCount = null
);
