namespace LankaConnect.Application.Common.Options;

/// <summary>
/// Configuration settings for event ticket commission calculation.
/// Phase 6A.71: Combined LankaConnect + Stripe platform fee on paid event tickets.
/// Phase 6A.X: Detailed revenue breakdown with separated Stripe fees and platform commission.
/// </summary>
public class CommissionSettings
{
    public const string SectionName = "CommissionSettings";

    /// <summary>
    /// Combined platform commission rate (LankaConnect + Stripe) - DEPRECATED.
    /// This is kept for backward compatibility with existing code.
    /// Default: 0.05 (5% total)
    ///
    /// Phase 6A.X: For new event creation with revenue breakdown, use the separated rates:
    /// - PlatformCommissionRate (2%)
    /// - StripeFeeRate (2.9%) + StripeFeeFixed ($0.30)
    /// </summary>
    public decimal EventTicketCommissionRate { get; init; } = 0.05m;

    /// <summary>
    /// Phase 6A.X: LankaConnect platform commission rate (separate from Stripe fees).
    /// Default: 0.02 (2%)
    /// This is the platform's revenue from each ticket sale.
    /// </summary>
    public decimal PlatformCommissionRate { get; init; } = 0.02m;

    /// <summary>
    /// Phase 6A.X: Stripe processing fee rate.
    /// Default: 0.029 (2.9%)
    /// This is Stripe's percentage fee per transaction.
    /// </summary>
    public decimal StripeFeeRate { get; init; } = 0.029m;

    /// <summary>
    /// Phase 6A.X: Stripe fixed fee per transaction.
    /// Default: 0.30 ($0.30)
    /// This is Stripe's fixed fee added to each transaction.
    /// </summary>
    public decimal StripeFeeFixed { get; init; } = 0.30m;

    /// <summary>
    /// Validates the commission settings.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when commission rates are invalid.</exception>
    public void Validate()
    {
        if (EventTicketCommissionRate < 0 || EventTicketCommissionRate >= 1)
        {
            throw new InvalidOperationException(
                $"EventTicketCommissionRate must be between 0 and 1 (got {EventTicketCommissionRate})");
        }

        if (PlatformCommissionRate < 0 || PlatformCommissionRate >= 1)
        {
            throw new InvalidOperationException(
                $"PlatformCommissionRate must be between 0 and 1 (got {PlatformCommissionRate})");
        }

        if (StripeFeeRate < 0 || StripeFeeRate >= 1)
        {
            throw new InvalidOperationException(
                $"StripeFeeRate must be between 0 and 1 (got {StripeFeeRate})");
        }

        if (StripeFeeFixed < 0)
        {
            throw new InvalidOperationException(
                $"StripeFeeFixed must be >= 0 (got {StripeFeeFixed})");
        }
    }
}
