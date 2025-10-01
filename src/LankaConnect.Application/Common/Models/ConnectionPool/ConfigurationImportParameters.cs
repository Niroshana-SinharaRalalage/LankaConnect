namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Parameters for importing connection pool configuration
/// </summary>
public class ConfigurationImportParameters
{
    /// <summary>
    /// Source of the configuration import (File, URL, Database)
    /// </summary>
    public string ImportSource { get; set; } = string.Empty;

    /// <summary>
    /// Content of configuration to import
    /// </summary>
    public string? ConfigurationContent { get; set; }

    /// <summary>
    /// File path for configuration import
    /// </summary>
    public string? ImportFilePath { get; set; }

    /// <summary>
    /// Whether to validate configuration before importing
    /// </summary>
    public bool ValidateBeforeImport { get; set; } = true;

    /// <summary>
    /// Whether to backup current configuration before import
    /// </summary>
    public bool CreateBackupBeforeImport { get; set; } = true;

    /// <summary>
    /// Specific configuration sections to import
    /// </summary>
    public List<string> IncludeSettings { get; set; } = new();

    /// <summary>
    /// Configuration sections to exclude from import
    /// </summary>
    public List<string> ExcludeSettings { get; set; } = new();
}