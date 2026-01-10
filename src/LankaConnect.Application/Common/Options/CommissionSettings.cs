namespace LankaConnect.Application.Common.Options;

/// <summary>
/// Configuration settings for event ticket commission calculation.
/// Phase 6A.71: Combined LankaConnect + Stripe platform fee on paid event tickets.
/// </summary>
public class CommissionSettings
{
    public const string SectionName = "CommissionSettings";

    /// <summary>
    /// Combined platform commission rate (LankaConnect + Stripe).
    /// Default: 0.05 (5% total)
    /// </summary>
    public decimal EventTicketCommissionRate { get; init; } = 0.05m;

    /// <summary>
    /// Validates the commission settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when commission rate is invalid.</exception>
    public void Validate()
    {
        if (EventTicketCommissionRate < 0 || EventTicketCommissionRate >= 1)
        {
            throw new InvalidOperationException(
                $"EventTicketCommissionRate must be between 0 and 1 (got {EventTicketCommissionRate})");
        }
    }
}
