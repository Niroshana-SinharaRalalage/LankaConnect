using LankaConnect.Domain.Shared.Types;

namespace LankaConnect.Application.Common.Models.Backup;

/// <summary>
/// Configuration for cultural intelligence model backups
/// </summary>
public class ModelBackupConfiguration
{
    /// <summary>
    /// Unique identifier for the backup configuration
    /// </summary>
    public Guid ConfigurationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Scope of the algorithm backup
    /// </summary>
    public AlgorithmBackupScope BackupScope { get; set; } = AlgorithmBackupScope.CulturalIntelligenceOnly;

    /// <summary>
    /// Frequency of automated backups
    /// </summary>
    public BackupFrequency BackupFrequency { get; set; } = BackupFrequency.Daily;

    /// <summary>
    /// Model types to include in backup
    /// </summary>
    public List<string> ModelTypes { get; set; } = new();

    /// <summary>
    /// Cultural communities whose models should be prioritized
    /// </summary>
    public List<string> PriorityCommunities { get; set; } = new();

    /// <summary>
    /// Retention period for backup versions
    /// </summary>
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromDays(90);

    /// <summary>
    /// Whether to include model training data in backup
    /// </summary>
    public bool IncludeTrainingData { get; set; } = false;

    /// <summary>
    /// Compression level for backup storage
    /// </summary>
    public string CompressionLevel { get; set; } = "Standard";

    /// <summary>
    /// Encryption settings for sensitive cultural data
    /// </summary>
    public Dictionary<string, string> EncryptionSettings { get; set; } = new();

    /// <summary>
    /// Maximum backup file size before splitting
    /// </summary>
    public long MaxBackupSizeBytes { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB
}