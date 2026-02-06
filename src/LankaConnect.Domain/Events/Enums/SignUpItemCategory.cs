namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Priority category for sign-up items
/// Replaces the binary Open/Predefined model with a flexible category-based system
/// Phase 6A.27: Added Open category for user-submitted items
/// </summary>
public enum SignUpItemCategory
{
    /// <summary>
    /// Mandatory items that MUST be brought by participants
    /// Example: Main dishes for potluck, essential decorations for temple event
    /// </summary>
    Mandatory = 0,

    /// <summary>
    /// Preferred items that are highly desired but not required
    /// Example: Side dishes, beverages, extra supplies
    /// Note: Deprecated in Phase 6A.27 - use Suggested instead
    /// Kept for backward compatibility with existing data
    /// </summary>
    [Obsolete("Use Suggested instead. Preferred is being deprecated.")]
    Preferred = 1,

    /// <summary>
    /// Suggested items that would be nice to have but are completely optional
    /// Example: Paper plates, extra chairs, decorative items
    /// </summary>
    Suggested = 2,

    /// <summary>
    /// Open items where users can add their own custom items
    /// Phase 6A.27: Users specify item name, description, and quantity
    /// Example: A user volunteers to bring "Homemade Cookies (qty: 50)"
    /// </summary>
    Open = 3
}
