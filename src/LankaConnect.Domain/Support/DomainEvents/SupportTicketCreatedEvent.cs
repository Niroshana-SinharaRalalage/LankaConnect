using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.89: Raised when a new support ticket is created from the contact form.
/// Used to trigger auto-confirmation email to the submitter.
/// </summary>
public record SupportTicketCreatedEvent(
    Guid TicketId,
    string ReferenceId,
    string Email,
    string Name,
    string Subject) : DomainEvent;
