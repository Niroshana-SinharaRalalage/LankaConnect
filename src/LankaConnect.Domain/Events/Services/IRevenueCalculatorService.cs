using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Phase 6A.X: Service for calculating detailed revenue breakdown for event tickets
/// </summary>
public interface IRevenueCalculatorService
{
    /// <summary>
    /// Calculates revenue breakdown for a ticket price using event location for tax calculation
    /// </summary>
    /// <param name="ticketPrice">Ticket price (gross amount buyer pays, tax-inclusive)</param>
    /// <param name="eventLocation">Event location for tax rate lookup (null = 0% tax)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete revenue breakdown with all components calculated</returns>
    Task<Result<RevenueBreakdown>> CalculateBreakdownAsync(
        Money ticketPrice,
        EventLocation? eventLocation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates revenue breakdown with explicit tax rate (for preview/testing)
    /// </summary>
    /// <param name="ticketPrice">Ticket price (gross amount buyer pays)</param>
    /// <param name="taxRate">Sales tax rate (e.g., 0.0725 for 7.25%)</param>
    /// <returns>Complete revenue breakdown</returns>
    Result<RevenueBreakdown> CalculateBreakdown(Money ticketPrice, decimal taxRate);
}
