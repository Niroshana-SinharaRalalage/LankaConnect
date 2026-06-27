using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Commands.UpdateEvent;

public record UpdateEventCommand(
    Guid EventId,
    string Title,
    string Description,
    // Phase 8YA.2: TBD-dates support. Both null is allowed and is interpreted as
    // "do not change the existing dates" (organiser is updating other fields).
    // Both set → SetDates path (validates + may transition Planning → Draft).
    // Mixed → validator rejects.
    DateTime? StartDate,
    DateTime? EndDate,
    int Capacity,
    // Issue #51: Max attendees per registration (optional)
    int? MaxAttendeesPerRegistration = null,
    EventCategory? Category = null,
    // Location (optional - null values will remove location)
    string? LocationAddress = null,
    string? LocationCity = null,
    string? LocationState = null,
    string? LocationZipCode = null,
    string? LocationCountry = null,
    decimal? LocationLatitude = null,
    decimal? LocationLongitude = null,
    // Phase 7C.1: Optional venue/location name (distinct from street address)
    string? LocationName = null,
    // Phase 7C.1: Secondary Location (optional — null type clears secondary location)
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
    // Session 33: Group Tiered Pricing - optional
    List<GroupPricingTierRequest>? GroupPricingTiers = null,
    // Phase 6A.32: Email Groups - optional
    List<Guid>? EmailGroupIds = null,
    // Organizer Contact Details - optional (supports multiple contacts, null = don't modify)
    bool? PublishOrganizerContact = null,
    List<OrganizerContactRequest>? OrganizerContacts = null,
    // IsFreeEvent fix: Explicit free event flag from frontend
    bool? IsFree = null,
    // Donation Feature: Optional donation configuration (null = don't modify)
    bool? DonationsEnabled = null,
    List<decimal>? DonationSuggestedAmounts = null,
    bool? DonationAllowCustomAmount = null,
    decimal? DonationMinAmount = null,
    decimal? DonationMaxAmount = null,
    string? DonationMessage = null,
    bool? ShowDonationSummary = null,
    // Phase 7E.2: Optional registration mode change.
    // Null means "don't modify". Domain rule (Event.SetRegistrationMode) forbids change once
    // registrations exist; the handler surfaces that as a 400 with a clear message.
    RegistrationMode? RegistrationMode = null,
    // Phase 8X — Optional payment-mode transition (Free / OnPlatformPaid / ExternalPaid).
    // Null means "don't modify". Domain rule (Event.SetPaymentMode / SetExternalPayment)
    // forbids change once active registrations exist.
    EventPaymentMode? PaymentMode = null,
    string? ExternalRegistrationUrl = null,
    string? ExternalRegistrationInstructions = null,
    string? ExternalRegistrationVendorName = null,
    // Phase 6A.154 — Tri-state vanity slug: UpdateVanitySlug=false (default)
    // means "don't touch", true means "apply VanitySlug" (including null to
    // clear). Same pattern as UpdateRegistrationWindow in 6A.153.
    bool UpdateVanitySlug = false,
    string? VanitySlug = null
) : ICommand;
