using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Common;

public record EventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public Guid OrganizerId { get; init; }
    public int Capacity { get; init; }
    public int CurrentRegistrations { get; init; }
    public EventStatus Status { get; init; }
    public EventCategory Category { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Location information (nullable - not all events have physical locations)
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }

    // Ticket pricing (nullable - free events)
    public decimal? TicketPriceAmount { get; init; }
    public Currency? TicketPriceCurrency { get; init; }
    public bool IsFree { get; init; }
}
