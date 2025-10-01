using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Sacred event recovery orchestrator interface for managing cultural event recovery operations
/// </summary>
public interface ISacredEventRecoveryOrchestrator
{
    /// <summary>
    /// Orchestrates recovery of sacred events
    /// </summary>
    Task<Result<SacredEventBackupResult>> OrchestrateSacredEventRecoveryAsync(
        string eventId,
        string recoveryPointId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates sacred event recovery readiness
    /// </summary>
    Task<Result<bool>> ValidateRecoveryReadinessAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available recovery points for sacred events
    /// </summary>
    Task<Result<List<SacredEventSnapshot>>> GetAvailableRecoveryPointsAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests sacred event recovery procedures
    /// </summary>
    Task<Result<bool>> TestRecoveryProceduresAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors sacred event recovery progress
    /// </summary>
    Task<Result<Dictionary<string, object>>> MonitorRecoveryProgressAsync(
        string recoveryOperationId,
        CancellationToken cancellationToken = default);
}