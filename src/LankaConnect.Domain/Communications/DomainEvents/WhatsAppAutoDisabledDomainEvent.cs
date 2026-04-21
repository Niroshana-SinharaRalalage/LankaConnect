using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Phase 7D Fix 4: raised when the background job (or any future administrative flow)
/// flips WhatsApp off on a user's behalf without their direct action. Consumed by the
/// application-layer handler that dispatches the "we turned WhatsApp off" email.
/// </summary>
public sealed record WhatsAppAutoDisabledDomainEvent(
    Guid UserId,
    string PhoneNumber,
    WhatsAppAutoDisableReason Reason) : DomainEvent;
