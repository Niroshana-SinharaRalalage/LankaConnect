namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Type of sign-up list for event items/volunteers
/// </summary>
public enum SignUpType
{
    /// <summary>
    /// Open sign-up - users can specify what they want to bring
    /// Example: "Bring any food dish you'd like"
    /// </summary>
    Open = 0,

    /// <summary>
    /// Predefined list - users must select from specific items
    /// Example: "Choose from: Rice, Curry, Dessert, Drinks"
    /// </summary>
    Predefined = 1
}
