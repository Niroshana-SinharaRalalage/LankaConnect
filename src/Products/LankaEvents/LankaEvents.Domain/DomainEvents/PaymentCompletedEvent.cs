using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.24: Domain event raised when payment is completed for a paid event registration.
/// This event triggers:
/// - Sending confirmation email with attendee details
/// - Generating ticket with QR code (for paid events)
/// </summary>
public record PaymentCompletedEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    string ContactEmail,
    string PaymentIntentId,
    decimal AmountPaid,
    int AttendeeCount,
    DateTime PaymentCompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
