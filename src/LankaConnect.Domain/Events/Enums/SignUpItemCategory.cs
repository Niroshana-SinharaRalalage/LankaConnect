namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Priority category for sign-up items
/// Replaces the binary Open/Predefined model with a flexible category-based system
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
    /// </summary>
    Preferred = 1,

    /// <summary>
    /// Suggested items that would be nice to have but are completely optional
    /// Example: Paper plates, extra chairs, decorative items
    /// </summary>
    Suggested = 2
}
