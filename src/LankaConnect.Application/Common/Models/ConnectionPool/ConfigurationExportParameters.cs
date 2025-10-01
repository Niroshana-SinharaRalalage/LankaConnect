namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Parameters for exporting connection pool configuration
/// </summary>
public class ConfigurationExportParameters
{
    /// <summary>
    /// Configuration format to export (JSON, XML, YAML)
    /// </summary>
    public string ExportFormat { get; set; } = "JSON";

    /// <summary>
    /// Whether to include sensitive information
    /// </summary>
    public bool IncludeSensitiveData { get; set; } = false;

    /// <summary>
    /// Specific configuration sections to export
    /// </summary>
    public List<string> IncludeSettings { get; set; } = new();

    /// <summary>
    /// Configuration sections to exclude from export
    /// </summary>
    public List<string> ExcludeSettings { get; set; } = new();

    /// <summary>
    /// Target file path for export
    /// </summary>
    public string? ExportFilePath { get; set; }
}