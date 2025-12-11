namespace LankaConnect.Infrastructure.Payments.Configuration;

/// <summary>
/// Configuration options for Stripe payment integration
/// </summary>
public class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Stripe publishable key (client-side)
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe secret key (server-side)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe webhook signing secret
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Trial period in days (default: 180 days = 6 months)
    /// </summary>
    public int TrialPeriodDays { get; set; } = 180;

    /// <summary>
    /// Default currency (USD)
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Pricing tiers configuration
    /// </summary>
    public PricingTiers PricingTiers { get; set; } = new();
}

/// <summary>
/// Pricing tier configuration
/// </summary>
public class PricingTiers
{
    /// <summary>
    /// General User pricing (in cents)
    /// </summary>
    public TierPricing General { get; set; } = new();

    /// <summary>
    /// Event Organizer pricing (in cents)
    /// </summary>
    public TierPricing EventOrganizer { get; set; } = new();
}

/// <summary>
/// Individual tier pricing
/// </summary>
public class TierPricing
{
    /// <summary>
    /// Monthly price in cents (e.g., 1000 = $10.00)
    /// </summary>
    public long MonthlyPrice { get; set; }

    /// <summary>
    /// Annual price in cents (e.g., 10000 = $100.00)
    /// </summary>
    public long AnnualPrice { get; set; }
}
