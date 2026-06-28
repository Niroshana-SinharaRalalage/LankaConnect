using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Commands.UpdateEventOrganizerContact;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateEvent;

public record CreateEventCommand(
    string Title,
    string Description,
    // Phase 8YA.2: TBD-dates support. Both null → Planning; both set → Draft;
    // mixed → validator rejects. Domain Event.Create enforces the same invariant
    // as a defence-in-depth.
    DateTime? StartDate,
    DateTime? EndDate,
    Guid OrganizerId,
    int Capacity,
    // Issue #51: Max attendees per registration (optional, defaults to 10)
    int? MaxAttendeesPerRegistration = null,
    EventCategory? Category = null,
    // Location (optional)
    string? LocationAddress = null,
    string? LocationCity = null,
    string? LocationState = null,
    string? LocationZipCode = null,
    string? LocationCountry = null,
    decimal? LocationLatitude = null,
    decimal? LocationLongitude = null,
    // Phase 7C.1: Optional venue/location name (distinct from street address)
    string? LocationName = null,
    // Phase 7C.1: Secondary Location (optional — null type means "no secondary")
    SecondaryLocationType? SecondaryLocationType = null,
    string? SecondaryLocationName = null,
    string? SecondaryLocationAddress = null,
    string? SecondaryLocationCity = null,
    string? SecondaryLocationState = null,
    string? SecondaryLocationZipCode = null,
    string? SecondaryLocationCountry = null,
    decimal? SecondaryLocationLatitude = null,
    decimal? SecondaryLocationLongitude = null,
    // Legacy Ticket Price (optional - backward compatibility)
    decimal? TicketPriceAmount = null,
    Currency? TicketPriceCurrency = null,
    // Session 21: Dual Pricing (Adult/Child) - optional
    decimal? AdultPriceAmount = null,
    Currency? AdultPriceCurrency = null,
    decimal? ChildPriceAmount = null,
    Currency? ChildPriceCurrency = null,
    int? ChildAgeLimit = null,
    // Phase 6D: Group Tiered Pricing - optional
    List<GroupPricingTierRequest>? GroupPricingTiers = null,
    // Phase 6A.32: Email Groups - optional
    List<Guid>? EmailGroupIds = null,
    // Organizer Contact Details - optional (supports multiple contacts)
    bool? PublishOrganizerContact = false,
    List<OrganizerContactRequest>? OrganizerContacts = null,
    // IsFreeEvent fix: Explicit free event flag from frontend
    bool? IsFree = null,
    // Donation Feature: Optional donation configuration
    bool? DonationsEnabled = null,
    List<decimal>? DonationSuggestedAmounts = null,
    bool? DonationAllowCustomAmount = null,
    decimal? DonationMinAmount = null,
    decimal? DonationMaxAmount = null,
    string? DonationMessage = null,
    bool? ShowDonationSummary = null,
    // Phase 7E.2: Per-event registration capture mode chosen by the organiser.
    // Defaults to DetailedAttendees (the pre-7E behaviour) so existing API clients that
    // do not yet send this field continue to work unchanged.
    RegistrationMode? RegistrationMode = null,
    // Phase 8X — Event payment mode (Free / OnPlatformPaid / ExternalPaid).
    // Optional — when null the validator infers per the inference table (security default
    // = OnPlatformPaid when IsFree != true, never silently Free per Phase 6A.81).
    EventPaymentMode? PaymentMode = null,
    // Phase 8X — External registration URL (required when PaymentMode = ExternalPaid).
    string? ExternalRegistrationUrl = null,
    // Phase 8X — Optional plain-text instructions rendered on the event detail page.
    string? ExternalRegistrationInstructions = null,
    // Phase 8X — Optional vendor name (e.g. "Eventbrite") used in CTA label.
    string? ExternalRegistrationVendorName = null,
    // Phase 6A.154 — Organizer-controlled vanity URL slug. Optional; null
    // means no vanity URL. Validated via EventVanitySlug.Create on the
    // handler side. Uniqueness enforced at DB level (partial unique index).
    string? VanitySlug = null
) : ICommand<Guid>;

/// <summary>
/// Phase 6D: Request model for a single group pricing tier
/// </summary>
public record GroupPricingTierRequest(
    int MinAttendees,
    int? MaxAttendees,
    decimal PricePerPerson,
    Currency Currency
);
