using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Backup orchestrator interface for coordinating backup operations
/// </summary>
public interface IBackupOrchestrator
{
    /// <summary>
    /// Initiates a backup operation
    /// </summary>
    Task<Result<BackupResult>> InitiateBackupAsync(
        string backupConfigurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a backup operation
    /// </summary>
    Task<Result<string>> GetBackupStatusAsync(
        string backupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a running backup operation
    /// </summary>
    Task<Result<bool>> CancelBackupAsync(
        string backupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all backup configurations
    /// </summary>
    Task<Result<List<CulturalIntelligenceBackupConfiguration>>> GetBackupConfigurationsAsync(
        CancellationToken cancellationToken = default);
}