using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Phase 6A.X: Service for retrieving US state sales tax rates
/// </summary>
public interface ISalesTaxService
{
    /// <summary>
    /// Gets the sales tax rate for a specific US state
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., "CA", "NY") or full state name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tax rate as decimal (e.g., 0.0725 for 7.25%), or Result with error if state not found</returns>
    Task<Result<decimal>> GetStateTaxRateAsync(string stateCode, CancellationToken cancellationToken = default);
}
