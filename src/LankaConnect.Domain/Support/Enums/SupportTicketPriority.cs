namespace LankaConnect.Domain.Support.Enums;

/// <summary>
/// Phase 6A.89: Support ticket priority levels
/// </summary>
public enum SupportTicketPriority
{
    /// <summary>
    /// Low priority - general inquiries
    /// </summary>
    Low = 1,

    /// <summary>
    /// Normal priority - standard support requests
    /// </summary>
    Normal = 2,

    /// <summary>
    /// High priority - urgent issues
    /// </summary>
    High = 3,

    /// <summary>
    /// Urgent priority - critical issues requiring immediate attention
    /// </summary>
    Urgent = 4
}
