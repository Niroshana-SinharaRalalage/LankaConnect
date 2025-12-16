using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Commands.UpdateEvent;

public record UpdateEventCommand(
    Guid EventId,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    int Capacity,
    EventCategory? Category = null,
    // Location (optional - null values will remove location)
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
    // Session 33: Group Tiered Pricing - optional
    List<GroupPricingTierRequest>? GroupPricingTiers = null,
    // Phase 6A.32: Email Groups - optional
    List<Guid>? EmailGroupIds = null
) : ICommand;
