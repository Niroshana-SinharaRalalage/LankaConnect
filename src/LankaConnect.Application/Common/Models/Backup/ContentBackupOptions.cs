namespace LankaConnect.Application.Common.Models.Backup;

/// <summary>
/// Options for backing up cultural content and community data
/// </summary>
public class ContentBackupOptions
{
    /// <summary>
    /// Types of content to include in backup
    /// </summary>
    public List<string> ContentTypes { get; set; } = new();

    /// <summary>
    /// Cultural sensitivity levels to backup (Sacred, Traditional, Public, Community)
    /// </summary>
    public List<string> SensitivityLevels { get; set; } = new();

    /// <summary>
    /// Geographic regions for content backup
    /// </summary>
    public List<string> GeographicRegions { get; set; } = new();

    /// <summary>
    /// Date range for content to backup
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date for content backup range
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Whether to include user-generated content
    /// </summary>
    public bool IncludeUserGeneratedContent { get; set; } = true;

    /// <summary>
    /// Whether to include multimedia content (images, videos)
    /// </summary>
    public bool IncludeMultimedia { get; set; } = true;

    /// <summary>
    /// Languages to include in content backup
    /// </summary>
    public List<string> Languages { get; set; } = new();

    /// <summary>
    /// Maximum file size for individual content items
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 100L * 1024 * 1024; // 100MB

    /// <summary>
    /// Whether to preserve original file metadata
    /// </summary>
    public bool PreserveMetadata { get; set; } = true;
}