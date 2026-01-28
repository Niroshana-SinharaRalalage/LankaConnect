using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Support;

/// <summary>
/// Phase 6A.89: Security audit log for admin actions.
/// Records all administrative actions for compliance and security tracking.
/// </summary>
public class AdminAuditLog : BaseEntity
{
    /// <summary>
    /// The admin user who performed the action
    /// </summary>
    public Guid AdminUserId { get; private set; }

    /// <summary>
    /// Type of action performed (e.g., "USER_LOCKED", "USER_DEACTIVATED", "TICKET_REPLIED")
    /// </summary>
    public string Action { get; private set; }

    /// <summary>
    /// The target user ID if the action was on a user (nullable for non-user actions)
    /// </summary>
    public Guid? TargetUserId { get; private set; }

    /// <summary>
    /// The target entity ID for non-user actions (e.g., ticket ID)
    /// </summary>
    public Guid? TargetEntityId { get; private set; }

    /// <summary>
    /// Type of target entity (e.g., "User", "SupportTicket")
    /// </summary>
    public string? TargetEntityType { get; private set; }

    /// <summary>
    /// JSON details with before/after state or additional context
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// IP address of the admin user
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// User agent of the admin user's browser/client
    /// </summary>
    public string? UserAgent { get; private set; }

    // EF Core constructor
    private AdminAuditLog()
    {
        Action = string.Empty;
    }

    /// <summary>
    /// Factory method to create an audit log for a user action
    /// </summary>
    public static AdminAuditLog CreateForUserAction(
        Guid adminUserId,
        string action,
        Guid targetUserId,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user ID is required", nameof(adminUserId));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));

        if (targetUserId == Guid.Empty)
            throw new ArgumentException("Target user ID is required", nameof(targetUserId));

        return new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action.Trim().ToUpperInvariant(),
            TargetUserId = targetUserId,
            TargetEntityId = null,
            TargetEntityType = "User",
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    /// <summary>
    /// Factory method to create an audit log for a support ticket action
    /// </summary>
    public static AdminAuditLog CreateForTicketAction(
        Guid adminUserId,
        string action,
        Guid ticketId,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user ID is required", nameof(adminUserId));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));

        if (ticketId == Guid.Empty)
            throw new ArgumentException("Ticket ID is required", nameof(ticketId));

        return new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action.Trim().ToUpperInvariant(),
            TargetUserId = null,
            TargetEntityId = ticketId,
            TargetEntityType = "SupportTicket",
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }

    /// <summary>
    /// Factory method to create an audit log for a generic action
    /// </summary>
    public static AdminAuditLog Create(
        Guid adminUserId,
        string action,
        Guid? targetEntityId = null,
        string? targetEntityType = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("Admin user ID is required", nameof(adminUserId));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));

        return new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action.Trim().ToUpperInvariant(),
            TargetUserId = null,
            TargetEntityId = targetEntityId,
            TargetEntityType = targetEntityType,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };
    }
}

/// <summary>
/// Phase 6A.89: Constants for admin audit action types
/// </summary>
public static class AdminAuditActions
{
    // User actions
    public const string UserLocked = "USER_LOCKED";
    public const string UserUnlocked = "USER_UNLOCKED";
    public const string UserDeactivated = "USER_DEACTIVATED";
    public const string UserActivated = "USER_ACTIVATED";
    public const string UserPasswordReset = "USER_PASSWORD_RESET";
    public const string UserVerificationResent = "USER_VERIFICATION_RESENT";

    // Support ticket actions
    public const string TicketReplied = "TICKET_REPLIED";
    public const string TicketStatusChanged = "TICKET_STATUS_CHANGED";
    public const string TicketAssigned = "TICKET_ASSIGNED";
    public const string TicketNoteAdded = "TICKET_NOTE_ADDED";
    public const string TicketPriorityChanged = "TICKET_PRIORITY_CHANGED";

    // Export actions
    public const string UsersExported = "USERS_EXPORTED";
}
