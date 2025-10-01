using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

/// <summary>
/// Value object representing a business image with multiple sizes and metadata
/// </summary>
public sealed class BusinessImage : ValueObject
{
    public string Id { get; private set; }
    public string OriginalUrl { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string MediumUrl { get; private set; }
    public string LargeUrl { get; private set; }
    public string AltText { get; private set; }
    public string Caption { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public long FileSizeBytes { get; private set; }
    public string ContentType { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; }

    private BusinessImage()
    {
        Id = string.Empty;
        OriginalUrl = string.Empty;
        ThumbnailUrl = string.Empty;
        MediumUrl = string.Empty;
        LargeUrl = string.Empty;
        AltText = string.Empty;
        Caption = string.Empty;
        ContentType = string.Empty;
        Metadata = new Dictionary<string, string>();
    }

    private BusinessImage(
        string id,
        string originalUrl,
        string thumbnailUrl,
        string mediumUrl,
        string largeUrl,
        string altText,
        string caption,
        int displayOrder,
        bool isPrimary,
        long fileSizeBytes,
        string contentType,
        DateTime uploadedAt,
        Dictionary<string, string>? metadata = null)
    {
        Id = id;
        OriginalUrl = originalUrl;
        ThumbnailUrl = thumbnailUrl;
        MediumUrl = mediumUrl;
        LargeUrl = largeUrl;
        AltText = altText;
        Caption = caption;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        FileSizeBytes = fileSizeBytes;
        ContentType = contentType;
        UploadedAt = uploadedAt;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Creates a new business image from upload results
    /// </summary>
    public static Result<BusinessImage> Create(
        string originalUrl,
        string thumbnailUrl,
        string mediumUrl,
        string largeUrl,
        string altText,
        string caption,
        int displayOrder,
        bool isPrimary,
        long fileSizeBytes,
        string contentType,
        Dictionary<string, string>? metadata = null)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(originalUrl))
            return Result<BusinessImage>.Failure("Original URL is required");

        if (string.IsNullOrWhiteSpace(thumbnailUrl))
            return Result<BusinessImage>.Failure("Thumbnail URL is required");

        if (string.IsNullOrWhiteSpace(mediumUrl))
            return Result<BusinessImage>.Failure("Medium URL is required");

        if (string.IsNullOrWhiteSpace(largeUrl))
            return Result<BusinessImage>.Failure("Large URL is required");

        if (string.IsNullOrWhiteSpace(contentType))
            return Result<BusinessImage>.Failure("Content type is required");

        if (displayOrder < 0)
            return Result<BusinessImage>.Failure("Display order must be non-negative");

        if (fileSizeBytes <= 0)
            return Result<BusinessImage>.Failure("File size must be greater than zero");

        if (!IsValidImageContentType(contentType))
            return Result<BusinessImage>.Failure("Invalid image content type");

        // Generate unique ID
        var id = Guid.NewGuid().ToString();

        var businessImage = new BusinessImage(
            id,
            originalUrl,
            thumbnailUrl,
            mediumUrl,
            largeUrl,
            altText ?? string.Empty,
            caption ?? string.Empty,
            displayOrder,
            isPrimary,
            fileSizeBytes,
            contentType,
            DateTime.UtcNow,
            metadata);

        return Result<BusinessImage>.Success(businessImage);
    }

    /// <summary>
    /// Updates the image metadata (alt text, caption, display order)
    /// </summary>
    public Result<BusinessImage> UpdateMetadata(string altText, string caption, int displayOrder)
    {
        if (displayOrder < 0)
            return Result<BusinessImage>.Failure("Display order must be non-negative");

        return Result<BusinessImage>.Success(new BusinessImage(
            Id,
            OriginalUrl,
            ThumbnailUrl,
            MediumUrl,
            LargeUrl,
            altText ?? string.Empty,
            caption ?? string.Empty,
            displayOrder,
            IsPrimary,
            FileSizeBytes,
            ContentType,
            UploadedAt,
            Metadata));
    }

    /// <summary>
    /// Sets this image as primary (only one image can be primary per business)
    /// </summary>
    public BusinessImage SetAsPrimary()
    {
        return new BusinessImage(
            Id,
            OriginalUrl,
            ThumbnailUrl,
            MediumUrl,
            LargeUrl,
            AltText,
            Caption,
            DisplayOrder,
            true,
            FileSizeBytes,
            ContentType,
            UploadedAt,
            Metadata);
    }

    /// <summary>
    /// Removes primary status from this image
    /// </summary>
    public BusinessImage RemovePrimaryStatus()
    {
        return new BusinessImage(
            Id,
            OriginalUrl,
            ThumbnailUrl,
            MediumUrl,
            LargeUrl,
            AltText,
            Caption,
            DisplayOrder,
            false,
            FileSizeBytes,
            ContentType,
            UploadedAt,
            Metadata);
    }

    /// <summary>
    /// Gets the appropriate image URL based on requested size
    /// </summary>
    public string GetImageUrl(ImageSize size = ImageSize.Medium)
    {
        return size switch
        {
            ImageSize.Thumbnail => ThumbnailUrl,
            ImageSize.Medium => MediumUrl,
            ImageSize.Large => LargeUrl,
            ImageSize.Original => OriginalUrl,
            _ => MediumUrl
        };
    }

    private static bool IsValidImageContentType(string contentType)
    {
        var validTypes = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/gif",
            "image/webp",
            "image/bmp",
            "image/tiff"
        };

        return validTypes.Contains(contentType.ToLowerInvariant());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return OriginalUrl;
        yield return ThumbnailUrl;
        yield return MediumUrl;
        yield return LargeUrl;
        yield return ContentType;
        yield return FileSizeBytes;
        yield return UploadedAt;
    }
}

/// <summary>
/// Enum for specifying image size requirements
/// </summary>
public enum ImageSize
{
    Thumbnail = 1,  // 150x150
    Medium = 2,     // 500x500
    Large = 3,      // 1200x1200
    Original = 4    // Original size
}