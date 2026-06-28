using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.81 Part 3: Raised when Preliminary registration created with pending payment.
/// Triggers email notification with payment link and 24h expiration notice.
///
/// Design Decision: Immediate email sending (user preference)
/// - Validation check in handler prevents sending if payment already completed
/// - Stripe checkout URLs auto-invalidate after successful payment (built-in protection)
/// - Fail-silent pattern: Email failures don't block registration transaction
/// </summary>
public record RegistrationPendingPaymentEvent(
    Guid EventId,
    Guid RegistrationId,
    Guid? UserId,
    string ContactEmail,
    string ContactName,
    string StripeCheckoutSessionId,
    DateTime CheckoutExpiresAt,
    decimal TotalAmount,
    string Currency,
    int AttendeeCount,
    DateTime CreatedAt
) : IDomainEvent
{
    public DateTime OccurredAt => CreatedAt;
}
