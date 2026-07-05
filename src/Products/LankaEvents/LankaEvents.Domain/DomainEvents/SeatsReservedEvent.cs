using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Domain event raised when seats are confirmed as reserved after payment completion.
/// </summary>
public record SeatsReservedEvent(
    Guid EventId,
    Guid RegistrationId,
    List<(Guid SeatId, int AttendeeIndex, string SeatLabel)> Seats
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
