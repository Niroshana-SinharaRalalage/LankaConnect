using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 6A.121: Dual nullable quantity fields — exactly ONE of PhysicalQuantity or
/// SlotsClaimed is populated based on item type.
/// Phase 7D.1: Added <see cref="Kind"/> so downstream handlers (email/WhatsApp) can
/// route volunteer-confirmation templates separately from item-signup templates.
/// </summary>
public record UserCommittedToSignUpEvent(
    Guid SignUpListId,
    Guid UserId,
    string ItemDescription,
    int? PhysicalQuantity,
    int? SlotsClaimed,
    DateTime OccurredAt,
    SignUpKind Kind = SignUpKind.Items) : IDomainEvent;
