using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.121: Dual nullable quantity fields — exactly ONE of PhysicalQuantity or
/// SlotsClaimed is populated based on item type.
/// Phase 7D.1: Added <see cref="Kind"/> so downstream handlers (email/WhatsApp) can
/// route volunteer-confirmation templates separately from item-signup templates.
/// Phase 6A.140: Added <see cref="ContactEmail"/> + <see cref="ContactName"/> so the
/// confirmation-email handler can fall back to the form-submitted contact when the
/// commitment was created anonymously (UserId is a deterministic GUID with no row in
/// the Users table — handler used to fail-silent for that case).
/// </summary>
public record UserCommittedToSignUpEvent(
    Guid SignUpListId,
    Guid UserId,
    string ItemDescription,
    int? PhysicalQuantity,
    int? SlotsClaimed,
    DateTime OccurredAt,
    SignUpKind Kind = SignUpKind.Items,
    string? ContactEmail = null,
    string? ContactName = null) : IDomainEvent;
