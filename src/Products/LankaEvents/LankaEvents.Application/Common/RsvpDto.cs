using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Common;

public record RsvpDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public int Quantity { get; init; }
    public RegistrationStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    // Event information for convenience
    public string? EventTitle { get; init; }
    public DateTime? EventStartDate { get; init; }
    public DateTime? EventEndDate { get; init; }
    public EventStatus? EventStatus { get; init; }
}
