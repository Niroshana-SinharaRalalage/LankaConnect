namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Result of configuration import operation
/// </summary>
public class ConfigurationImportResult
{
    /// <summary>
    /// Whether the import was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of configuration settings imported
    /// </summary>
    public int ImportedConfigurationCount { get; set; }

    /// <summary>
    /// Number of configuration settings skipped or failed
    /// </summary>
    public int SkippedConfigurationCount { get; set; }

    /// <summary>
    /// Validation errors found during import
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Error message if import failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Path to backup file created before import
    /// </summary>
    public string? BackupFilePath { get; set; }

    /// <summary>
    /// Timestamp when import completed
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}