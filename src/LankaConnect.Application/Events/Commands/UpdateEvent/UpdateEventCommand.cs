using LankaConnect.Application.Common.Interfaces;
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
    // Ticket Price (optional - null values will make event free)
    decimal? TicketPriceAmount = null,
    Currency? TicketPriceCurrency = null
) : ICommand;
