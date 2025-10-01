namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Result of configuration export operation
/// </summary>
public class ConfigurationExportResult
{
    /// <summary>
    /// Whether the export was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Exported configuration content
    /// </summary>
    public string? ExportedContent { get; set; }

    /// <summary>
    /// File path where configuration was saved
    /// </summary>
    public string? ExportedFilePath { get; set; }

    /// <summary>
    /// Size of exported data in bytes
    /// </summary>
    public long ExportedDataSize { get; set; }

    /// <summary>
    /// Error message if export failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when export completed
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}