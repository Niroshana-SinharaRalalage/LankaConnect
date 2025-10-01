using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Revenue protection service interface for safeguarding business operations during disasters
/// </summary>
public interface IRevenueProtectionService
{
    /// <summary>
    /// Activates revenue protection measures
    /// </summary>
    Task<Result<string>> ActivateRevenueProtectionAsync(
        string triggerEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current revenue protection status
    /// </summary>
    Task<Result<Dictionary<string, object>>> GetRevenueProtectionStatusAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates potential revenue impact
    /// </summary>
    Task<Result<decimal>> CalculateRevenueImpactAsync(
        string disasterScenario,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates revenue protection readiness
    /// </summary>
    Task<Result<bool>> ValidateRevenueProtectionReadinessAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revenue protection metrics
    /// </summary>
    Task<Result<Dictionary<string, decimal>>> GetRevenueProtectionMetricsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}