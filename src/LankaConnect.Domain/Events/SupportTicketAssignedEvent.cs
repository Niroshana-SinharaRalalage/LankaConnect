using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.89: Raised when a support ticket is assigned to an admin user.
/// Used to notify the assigned admin about the new assignment.
/// </summary>
public record SupportTicketAssignedEvent(
    Guid TicketId,
    string ReferenceId,
    Guid AssignedToUserId,
    Guid? PreviousAssigneeUserId) : DomainEvent;
