using LankaConnect.Application.Common.Interfaces;
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
    EventCategory? Category = null,
    // Location (optional)
    string? LocationAddress = null,
    string? LocationCity = null,
    string? LocationState = null,
    string? LocationZipCode = null,
    string? LocationCountry = null,
    decimal? LocationLatitude = null,
    decimal? LocationLongitude = null,
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
    List<GroupPricingTierRequest>? GroupPricingTiers = null
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
