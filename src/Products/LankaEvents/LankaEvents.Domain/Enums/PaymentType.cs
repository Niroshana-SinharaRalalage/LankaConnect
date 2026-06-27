namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Type of payment made for a registration.
/// Used to distinguish between initial registration payments and subsequent additions.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public enum PaymentType
{
    /// <summary>
    /// Initial payment made during registration creation.
    /// </summary>
    Initial = 0,

    /// <summary>
    /// Additional payment made when adding attendees to an existing registration.
    /// </summary>
    Addition = 1
}
