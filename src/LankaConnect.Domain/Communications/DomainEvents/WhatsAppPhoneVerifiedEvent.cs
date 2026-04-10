using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Phase 7A: Domain event raised when a user verifies their WhatsApp phone number.
/// </summary>
public sealed record WhatsAppPhoneVerifiedEvent(
    Guid UserId,
    string PhoneNumber) : DomainEvent;
