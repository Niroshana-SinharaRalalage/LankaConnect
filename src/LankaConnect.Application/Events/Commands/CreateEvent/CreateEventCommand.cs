using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Commands.CreateEvent;

public record CreateEventCommand(
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
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
    RegistrationMode? RegistrationMode = null
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
