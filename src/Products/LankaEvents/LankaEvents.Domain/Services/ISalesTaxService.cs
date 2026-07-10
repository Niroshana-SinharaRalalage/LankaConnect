using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.Services;

/// <summary>
/// Phase 6A.X: Service for retrieving US state sales tax rates
/// Phase 6A.95: Added feature flag support for enabling/disabling tax collection
/// </summary>
public interface ISalesTaxService
{
    /// <summary>
    /// Gets the sales tax rate for a specific US state.
    /// Returns 0 if the sales tax feature is disabled.
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., "CA", "NY") or full state name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tax rate as decimal (e.g., 0.0725 for 7.25%), 0 if feature disabled, or Result with error if state not found</returns>
    Task<Result<decimal>> GetStateTaxRateAsync(string stateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.95: Indicates whether sales tax collection is currently enabled.
    /// When false, GetStateTaxRateAsync will always return 0.
    /// </summary>
    bool IsEnabled { get; }
}
