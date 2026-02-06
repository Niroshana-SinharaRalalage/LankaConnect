using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Tax.Repositories;

/// <summary>
/// Phase 6A.X: Repository interface for StateTaxRate entity
/// </summary>
public interface IStateTaxRateRepository : IRepository<StateTaxRate>
{
    /// <summary>
    /// Gets the active tax rate for a specific state by state code
    /// </summary>
    /// <param name="stateCode">Two-letter state code (e.g., "CA", "NY")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active StateTaxRate for the state, or null if not found</returns>
    Task<StateTaxRate?> GetActiveByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active tax rates
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all active state tax rates</returns>
    Task<IReadOnlyList<StateTaxRate>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tax rate history for a specific state
    /// </summary>
    /// <param name="stateCode">Two-letter state code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all tax rates for the state, ordered by effective date descending</returns>
    Task<IReadOnlyList<StateTaxRate>> GetHistoryByStateCodeAsync(string stateCode, CancellationToken cancellationToken = default);
}
