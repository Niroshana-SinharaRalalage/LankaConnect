namespace LankaConnect.Domain.Support.Enums;

/// <summary>
/// Phase 6A.89: Support ticket status workflow
/// </summary>
public enum SupportTicketStatus
{
    /// <summary>
    /// Ticket has been created but not yet viewed by admin
    /// </summary>
    New = 1,

    /// <summary>
    /// Ticket is being worked on by admin
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Admin has replied, waiting for customer response
    /// </summary>
    WaitingForResponse = 3,

    /// <summary>
    /// Issue has been resolved
    /// </summary>
    Resolved = 4,

    /// <summary>
    /// Ticket has been closed (cannot be reopened)
    /// </summary>
    Closed = 5
}
