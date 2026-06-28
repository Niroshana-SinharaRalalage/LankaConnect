using LankaConnect.Domain.Common;
using LankaConnect.Domain.Support.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.89: Raised when a support ticket status changes.
/// Used for audit logging and potential notifications.
/// </summary>
public record SupportTicketStatusChangedEvent(
    Guid TicketId,
    string ReferenceId,
    SupportTicketStatus OldStatus,
    SupportTicketStatus NewStatus) : DomainEvent;
