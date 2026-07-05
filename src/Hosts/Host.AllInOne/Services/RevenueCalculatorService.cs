using LankaConnect.Application.Common.Options;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Options;
using Serilog;
namespace LankaConnect.Host.AllInOne.Services;

/// <summary>
/// Phase 6A.X: Service for calculating detailed revenue breakdowns for event tickets
/// Phase 6A.95: Added sales tax feature flag support
/// Uses configurable fee rates from CommissionSettings
/// </summary>
public class RevenueCalculatorService : IRevenueCalculatorService
{
    private readonly ISalesTaxService _salesTaxService;
    private readonly CommissionSettings _commissionSettings;
    private readonly SalesTaxSettings _salesTaxSettings;
    private readonly ILogger _logger;

    /// <summary>
    /// Phase 6A.95: Indicates whether sales tax collection is currently enabled.
    /// Used by API controllers to expose feature flag status to frontend.
    /// </summary>
    public bool IsSalesTaxEnabled => _salesTaxSettings.Enabled;

    public RevenueCalculatorService(
        ISalesTaxService salesTaxService,
        IOptions<CommissionSettings> commissionSettings,
        IOptions<SalesTaxSettings> salesTaxSettings,
        ILogger logger)
    {
        _salesTaxService = salesTaxService;
        _commissionSettings = commissionSettings.Value;
        _salesTaxSettings = salesTaxSettings.Value;
        _logger = logger;

        _logger.Information(
            "RevenueCalculatorService initialized. SalesTaxEnabled={SalesTaxEnabled}, " +
            "Rates: PlatformCommission={Platform}%, StripeFee={Stripe}% + ${Fixed}",
            _salesTaxSettings.Enabled,
            _commissionSettings.PlatformCommissionRate * 100,
            _commissionSettings.StripeFeeRate * 100,
            _commissionSettings.StripeFeeFixed);
    }

    public async Task<Result<RevenueBreakdown>> CalculateBreakdownAsync(
        Money ticketPrice,
        EventLocation? eventLocation,
        CancellationToken cancellationToken = default)
    {
        if (ticketPrice == null)
            return Result<RevenueBreakdown>.Failure("Ticket price is required");

        // Determine tax rate based on event location
        decimal taxRate = 0m;

        if (eventLocation != null)
        {
            // Only calculate tax for US events with valid state
            if (eventLocation.Address?.Country == "United States" &&
                !string.IsNullOrWhiteSpace(eventLocation.Address?.State))
            {
                var taxRateResult = await _salesTaxService.GetStateTaxRateAsync(
                    eventLocation.Address.State,
                    cancellationToken);

                if (taxRateResult.IsFailure)
                {
                    _logger.Warning("Failed to get tax rate for state {State}: {Error}",
                        eventLocation.Address.State, taxRateResult.Error);
                    // Continue with 0% tax rate instead of failing
                    taxRate = 0m;
                }
                else
                {
                    taxRate = taxRateResult.Value;
                    _logger.Debug("Retrieved tax rate {TaxRate} for state {State}",
                        taxRate, eventLocation.Address.State);
                }
            }
            else if (eventLocation.Address?.Country != "United States")
            {
                _logger.Information("International event detected (Country: {Country}), using 0% tax rate",
                    eventLocation.Address?.Country);
            }
        }
        else
        {
            _logger.Information("No event location provided, using 0% tax rate");
        }

        // Calculate breakdown with determined tax rate
        return CalculateBreakdown(ticketPrice, taxRate);
    }

    public Result<RevenueBreakdown> CalculateBreakdown(Money ticketPrice, decimal taxRate)
    {
        if (ticketPrice == null)
            return Result<RevenueBreakdown>.Failure("Ticket price is required");

        _logger.Debug(
            "Calculating revenue breakdown for ticket price {Price} with tax rate {TaxRate}. " +
            "Using configured rates: Platform={Platform}%, Stripe={Stripe}% + ${Fixed}",
            ticketPrice.Amount,
            taxRate,
            _commissionSettings.PlatformCommissionRate * 100,
            _commissionSettings.StripeFeeRate * 100,
            _commissionSettings.StripeFeeFixed);

        // Use configured rates from CommissionSettings
        var breakdownResult = RevenueBreakdown.Create(
            ticketPrice,
            taxRate,
            _commissionSettings.PlatformCommissionRate,
            _commissionSettings.StripeFeeRate,
            _commissionSettings.StripeFeeFixed);

        if (breakdownResult.IsSuccess)
        {
            _logger.Information(
                "Revenue breakdown calculated: Gross={Gross}, Tax={Tax}, Stripe={Stripe}, Commission={Commission}, Payout={Payout}",
                breakdownResult.Value.GrossAmount.Amount,
                breakdownResult.Value.SalesTaxAmount.Amount,
                breakdownResult.Value.StripeFeeAmount.Amount,
                breakdownResult.Value.PlatformCommission.Amount,
                breakdownResult.Value.OrganizerPayout.Amount);
        }
        else
        {
            _logger.Warning("Revenue breakdown calculation failed: {Error}", breakdownResult.Error);
        }

        return breakdownResult;
    }
}
