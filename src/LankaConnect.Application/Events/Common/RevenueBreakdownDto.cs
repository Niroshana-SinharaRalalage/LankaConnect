using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.X: DTO for revenue breakdown showing detailed fee structure
/// Displays how ticket price is allocated between tax, fees, and organizer payout
/// </summary>
public record RevenueBreakdownDto
{
    /// <summary>
    /// Gross amount (ticket price) paid by buyer (tax-inclusive)
    /// </summary>
    public decimal GrossAmount { get; init; }

    /// <summary>
    /// Sales tax amount (state tax based on event location)
    /// </summary>
    public decimal SalesTaxAmount { get; init; }

    /// <summary>
    /// Taxable amount (gross minus sales tax)
    /// This is the base for calculating Stripe fees and platform commission
    /// </summary>
    public decimal TaxableAmount { get; init; }

    /// <summary>
    /// Stripe payment processing fee (2.9% + $0.30)
    /// </summary>
    public decimal StripeFeeAmount { get; init; }

    /// <summary>
    /// Platform commission (2% of taxable amount)
    /// </summary>
    public decimal PlatformCommissionAmount { get; init; }

    /// <summary>
    /// Net amount to event organizer after all fees and taxes
    /// </summary>
    public decimal OrganizerPayoutAmount { get; init; }

    /// <summary>
    /// Currency for all amounts
    /// </summary>
    public Currency Currency { get; init; }

    /// <summary>
    /// Sales tax rate as decimal (e.g., 0.0725 for 7.25%)
    /// </summary>
    public decimal SalesTaxRate { get; init; }

    /// <summary>
    /// Display-friendly tax rate percentage (e.g., "7.25%")
    /// </summary>
    public string TaxRateDisplay { get; init; } = string.Empty;

    /// <summary>
    /// State/jurisdiction where tax was calculated
    /// </summary>
    public string? TaxJurisdiction { get; init; }
}
