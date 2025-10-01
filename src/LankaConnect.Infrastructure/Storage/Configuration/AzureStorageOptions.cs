namespace LankaConnect.Infrastructure.Storage.Configuration;

/// <summary>
/// Configuration options for Azure Storage services
/// </summary>
public sealed class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    /// <summary>
    /// Azure Storage connection string (for production)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Container name for business images
    /// </summary>
    public string BusinessImagesContainer { get; set; } = "business-images";

    /// <summary>
    /// Base URL for Azure Storage account (optional, auto-detected if not provided)
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum file size in bytes (default: 10MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>
    /// Allowed image MIME types
    /// </summary>
    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/jpg", 
        "image/png",
        "image/gif",
        "image/webp",
        "image/bmp"
    };

    /// <summary>
    /// Image resize settings for different sizes
    /// </summary>
    public ImageSizeSettings ImageSizes { get; set; } = new();

    /// <summary>
    /// CDN configuration for optimized image delivery
    /// </summary>
    public CdnSettings? Cdn { get; set; }

    /// <summary>
    /// Environment-specific settings
    /// </summary>
    public bool IsDevelopment { get; set; }

    /// <summary>
    /// Azurite settings for local development
    /// </summary>
    public AzuriteSettings Azurite { get; set; } = new();
}

/// <summary>
/// Image resize configuration for different sizes
/// </summary>
public sealed class ImageSizeSettings
{
    public ImageSizeConfig Thumbnail { get; set; } = new() { Width = 150, Height = 150, Quality = 80 };
    public ImageSizeConfig Medium { get; set; } = new() { Width = 500, Height = 500, Quality = 85 };
    public ImageSizeConfig Large { get; set; } = new() { Width = 1200, Height = 1200, Quality = 90 };
}

/// <summary>
/// Configuration for a specific image size
/// </summary>
public sealed class ImageSizeConfig
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int Quality { get; set; } = 85;
    public bool MaintainAspectRatio { get; set; } = true;
}

/// <summary>
/// CDN configuration for optimized image delivery
/// </summary>
public sealed class CdnSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int CacheDurationHours { get; set; } = 24;
}

/// <summary>
/// Azurite configuration for local development
/// </summary>
public sealed class AzuriteSettings
{
    public string ConnectionString { get; set; } = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;";
    public string BlobEndpoint { get; set; } = "http://127.0.0.1:10000/devstoreaccount1";
}