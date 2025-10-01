using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;
using LankaConnect.Application.Common.Models;
using LankaConnect.Domain.CulturalIntelligence;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Disaster recovery service interface for managing recovery operations
/// </summary>
public interface IDisasterRecoveryService
{
    /// <summary>
    /// Initiates disaster recovery process
    /// </summary>
    Task<Result<string>> InitiateRecoveryAsync(
        FailoverRequest failoverRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a recovery operation
    /// </summary>
    Task<Result<string>> GetRecoveryStatusAsync(
        string recoveryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates recovery readiness for a region
    /// </summary>
    Task<Result<bool>> ValidateRecoveryReadinessAsync(
        string region,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests failover capabilities
    /// </summary>
    Task<Result<bool>> TestFailoverAsync(
        string sourceRegion,
        string targetRegion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available recovery points
    /// </summary>
    Task<Result<List<string>>> GetRecoveryPointsAsync(
        string region,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute cross-region language routing failover for disaster recovery
    /// Maintains <60 second failover time as per Cultural Intelligence ADR
    /// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
    /// </summary>
    /// <param name="failoverContext">Disaster recovery failover context</param>
    /// <returns>Failover execution status with cultural intelligence preservation</returns>
    Task<LankaConnect.Application.Common.Models.LanguageRoutingFailoverResult> ExecuteCrossRegionLanguageRoutingFailoverAsync(LankaConnect.Application.Common.Models.Routing.DisasterRecoveryFailoverContext failoverContext);

    /// <summary>
    /// Preserve cultural intelligence state during disaster recovery scenarios
    /// Ensures sacred event continuity and diaspora community service preservation
    /// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
    /// </summary>
    /// <param name="culturalIntelligenceState">Current cultural intelligence state</param>
    /// <param name="targetRegion">Target region for state replication</param>
    /// <returns>Cultural intelligence preservation status</returns>
    Task<LankaConnect.Application.Common.Models.CulturalIntelligencePreservationResult> PreserveCulturalIntelligenceStateAsync(
        LankaConnect.Domain.CulturalIntelligence.CulturalIntelligenceState culturalIntelligenceState,
        LankaConnect.Domain.Common.Database.CulturalRegion targetRegion);
}