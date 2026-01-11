namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Newsletter lifecycle status
/// Phase 6A.74: Newsletter/News Alert Feature
///
/// Workflow: Draft → Active → Inactive/Sent
/// - Draft: Initial state, editable
/// - Active: Published and visible in system, expires after 7 days
/// - Inactive: Auto-deactivated after 7 days or manually deactivated
/// - Sent: Final state, emails delivered
/// </summary>
public enum NewsletterStatus
{
    /// <summary>
    /// Newsletter is in draft state, not yet published
    /// Can be edited, published, or deleted
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Newsletter is published and active
    /// Visible in system, email can be sent
    /// Auto-deactivates after 7 days
    /// </summary>
    Active = 2,

    /// <summary>
    /// Newsletter has been deactivated (auto or manual)
    /// Not visible in public areas, can be reactivated
    /// </summary>
    Inactive = 3,

    /// <summary>
    /// Newsletter emails have been sent
    /// Final state, cannot be reactivated
    /// </summary>
    Sent = 4
}
