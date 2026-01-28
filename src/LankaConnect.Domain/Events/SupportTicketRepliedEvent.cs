using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.89: Raised when an admin replies to a support ticket.
/// Used to send reply notification email to the submitter.
/// </summary>
public record SupportTicketRepliedEvent(
    Guid TicketId,
    string ReferenceId,
    string Email,
    string Name,
    string Subject,
    string ReplyContent,
    Guid RepliedByUserId) : DomainEvent;
