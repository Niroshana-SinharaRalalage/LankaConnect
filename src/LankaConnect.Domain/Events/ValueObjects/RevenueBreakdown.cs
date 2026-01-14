using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Phase 6A.X: Value object representing detailed revenue breakdown for event tickets
/// Calculates tax-inclusive pricing breakdown showing sales tax, Stripe fees, platform commission,
/// and final organizer payout
/// </summary>
public class RevenueBreakdown : ValueObject
{
    /// <summary>
    /// Gross amount (what the buyer pays, including tax)
    /// </summary>
    public Money GrossAmount { get; private set; }

    /// <summary>
    /// Sales tax amount extracted from gross amount (state-level)
    /// </summary>
    public Money SalesTaxAmount { get; private set; }

    /// <summary>
    /// Taxable amount (gross amount minus sales tax)
    /// </summary>
    public Money TaxableAmount { get; private set; }

    /// <summary>
    /// Stripe processing fee (2.9% + $0.30 by default)
    /// </summary>
    public Money StripeFeeAmount { get; private set; }

    /// <summary>
    /// Platform commission (2% of taxable amount by default)
    /// </summary>
    public Money PlatformCommission { get; private set; }

    /// <summary>
    /// Final payout to event organizer after all deductions
    /// </summary>
    public Money OrganizerPayout { get; private set; }

    /// <summary>
    /// Sales tax rate applied (e.g., 0.0725 for 7.25%)
    /// </summary>
    public decimal SalesTaxRate { get; private set; }

    /// <summary>
    /// Stripe fee rate (e.g., 0.029 for 2.9%)
    /// </summary>
    public decimal StripeFeeRate { get; private set; }

    /// <summary>
    /// Stripe fixed fee per transaction (e.g., 0.30 for $0.30)
    /// </summary>
    public decimal StripeFeeFixed { get; private set; }

    /// <summary>
    /// Platform commission rate (e.g., 0.02 for 2%)
    /// </summary>
    public decimal PlatformCommissionRate { get; private set; }

    // EF Core constructor
    private RevenueBreakdown()
    {
        // Required for EF Core
        GrossAmount = null!;
        SalesTaxAmount = null!;
        TaxableAmount = null!;
        StripeFeeAmount = null!;
        PlatformCommission = null!;
        OrganizerPayout = null!;
    }

    private RevenueBreakdown(
        Money grossAmount,
        Money salesTaxAmount,
        Money taxableAmount,
        Money stripeFeeAmount,
        Money platformCommission,
        Money organizerPayout,
        decimal salesTaxRate,
        decimal stripeFeeRate,
        decimal stripeFeeFixed,
        decimal platformCommissionRate)
    {
        GrossAmount = grossAmount;
        SalesTaxAmount = salesTaxAmount;
        TaxableAmount = taxableAmount;
        StripeFeeAmount = stripeFeeAmount;
        PlatformCommission = platformCommission;
        OrganizerPayout = organizerPayout;
        SalesTaxRate = salesTaxRate;
        StripeFeeRate = stripeFeeRate;
        StripeFeeFixed = stripeFeeFixed;
        PlatformCommissionRate = platformCommissionRate;
    }

    /// <summary>
    /// Creates a revenue breakdown with tax-inclusive pricing calculation
    /// Formula:
    /// 1. Gross Amount (GA) = ticket price (what buyer pays)
    /// 2. Sales Tax (ST) = GA - (GA / (1 + TaxRate))
    /// 3. Taxable Amount (TA) = GA - ST
    /// 4. Stripe Fee (SF) = (TA × StripeFeeRate) + StripeFeeFixed
    /// 5. Platform Commission (PC) = TA × PlatformCommissionRate
    /// 6. Organizer Payout (OP) = TA - SF - PC
    /// </summary>
    /// <param name="grossAmount">Total ticket price including tax (what buyer pays)</param>
    /// <param name="salesTaxRate">Sales tax rate (e.g., 0.0725 for 7.25%)</param>
    /// <param name="platformCommissionRate">Platform commission rate (default: 0.02 for 2%)</param>
    /// <param name="stripeFeeRate">Stripe percentage fee rate (default: 0.029 for 2.9%)</param>
    /// <param name="stripeFeeFixed">Stripe fixed fee per transaction (default: 0.30)</param>
    /// <returns>Revenue breakdown with calculated components</returns>
    public static Result<RevenueBreakdown> Create(
        Money grossAmount,
        decimal salesTaxRate,
        decimal platformCommissionRate = 0.02m,
        decimal stripeFeeRate = 0.029m,
        decimal stripeFeeFixed = 0.30m)
    {
        // Validation
        if (grossAmount == null)
            return Result<RevenueBreakdown>.Failure("Gross amount is required");

        if (salesTaxRate < 0 || salesTaxRate > 0.5m)
            return Result<RevenueBreakdown>.Failure("Sales tax rate must be between 0 and 50%");

        var currency = grossAmount.Currency;

        // Handle zero/free tickets
        if (grossAmount.IsZero)
        {
            var zero = Money.Zero(currency);
            return Result<RevenueBreakdown>.Success(new RevenueBreakdown(
                grossAmount,
                zero,
                zero,
                zero,
                zero,
                zero,
                salesTaxRate,
                stripeFeeRate,
                stripeFeeFixed,
                platformCommissionRate
            ));
        }

        // Calculate breakdown using tax-inclusive formula
        // 1. Extract sales tax from gross amount (reverse calculation)
        var preTaxAmount = grossAmount.Amount / (1 + salesTaxRate);
        var salesTaxAmount = grossAmount.Amount - preTaxAmount;

        var salesTax = Money.Create(salesTaxAmount, currency).Value;
        var taxableAmount = Money.Create(preTaxAmount, currency).Value;

        // 2. Calculate Stripe fee on taxable amount
        var stripeFeeAmountCalculated = (preTaxAmount * stripeFeeRate) + stripeFeeFixed;
        var stripeFee = Money.Create(stripeFeeAmountCalculated, currency).Value;

        // 3. Calculate platform commission on taxable amount
        var platformCommissionAmountCalculated = preTaxAmount * platformCommissionRate;
        var platformCommission = Money.Create(platformCommissionAmountCalculated, currency).Value;

        // 4. Calculate organizer payout
        var payoutAmount = preTaxAmount - stripeFeeAmountCalculated - platformCommissionAmountCalculated;

        // Validation: Payout cannot be negative
        if (payoutAmount < 0)
            return Result<RevenueBreakdown>.Failure(
                $"Revenue breakdown results in negative payout (${payoutAmount:F2}). " +
                "Ticket price is too low to cover transaction fees. Consider increasing the price.");

        var payout = Money.Create(payoutAmount, currency).Value;

        return Result<RevenueBreakdown>.Success(new RevenueBreakdown(
            grossAmount,
            salesTax,
            taxableAmount,
            stripeFee,
            platformCommission,
            payout,
            salesTaxRate,
            stripeFeeRate,
            stripeFeeFixed,
            platformCommissionRate
        ));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return GrossAmount;
        yield return SalesTaxAmount;
        yield return TaxableAmount;
        yield return StripeFeeAmount;
        yield return PlatformCommission;
        yield return OrganizerPayout;
        yield return SalesTaxRate;
        yield return StripeFeeRate;
        yield return StripeFeeFixed;
        yield return PlatformCommissionRate;
    }
}
