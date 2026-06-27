namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Type of sponsorship: monetary (via Stripe) or item-based (no payment).
/// </summary>
public enum SponsorType
{
    /// <summary>
    /// Monetary sponsorship processed via Stripe payment.
    /// Follows the same payment lifecycle as donations.
    /// </summary>
    Money = 0,

    /// <summary>
    /// Item-based sponsorship (e.g., providing venue, food, equipment).
    /// No payment required — recorded immediately with item details.
    /// </summary>
    Item = 1
}
