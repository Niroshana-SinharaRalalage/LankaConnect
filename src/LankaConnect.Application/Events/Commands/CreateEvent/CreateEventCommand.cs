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
    // Ticket Price (optional)
    decimal? TicketPriceAmount = null,
    Currency? TicketPriceCurrency = null
) : ICommand<Guid>;
