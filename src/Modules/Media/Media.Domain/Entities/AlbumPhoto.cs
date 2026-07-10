using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Media.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace LankaConnect.Modules.Media.Domain.Entities;

/// <summary>
/// Represents a media item (photo or video) in an event's post-event photo album.
/// Entity within PhotoAlbum aggregate — lifecycle controlled by PhotoAlbum.
/// Photos: stores 3 image sizes (original, medium 800px, thumbnail 150px).
/// Videos: stores original video + thumbnail only (no medium variant).
/// Media auto-expires after RetentionDays (default 7 days).
/// All media is immediately approved (no moderation).
/// </summary>
public class AlbumPhoto : LegacyBaseEntity
{
    public Guid AlbumId { get; private set; }
    public Guid UploaderId { get; private set; }
    public string UploaderName { get; private set; }        // Denormalized for display without user lookup
    public string OriginalUrl { get; private set; }          // Full-size image or original video
    public string OriginalBlobName { get; private set; }
    public string ThumbnailUrl { get; private set; }         // 150x150 center-crop WebP
    public string ThumbnailBlobName { get; private set; }
    public string? MediumUrl { get; private set; }           // 800px wide WebP (photos only, null for videos)
    public string? MediumBlobName { get; private set; }
    public string? Caption { get; private set; }             // Optional caption (max 500 chars)
    public AlbumPhotoStatus Status { get; private set; }
    public AlbumMediaType MediaType { get; private set; }    // Photo or Video discriminator
    public long FileSizeBytes { get; private set; }
    public long? DurationSeconds { get; private set; }       // Video duration (null for photos)
    public DateTime UploadedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }          // Auto-deletion date
    public int DisplayOrder { get; private set; }

    public const int MAX_CAPTION_LENGTH = 500;

    /// <summary>Whether this media item is a video.</summary>
    public bool IsVideo => MediaType == AlbumMediaType.Video;

    // EF Core constructor
    [SetsRequiredMembers]
    private AlbumPhoto()
    {
        UploaderName = null!;
        OriginalUrl = null!;
        OriginalBlobName = null!;
        ThumbnailUrl = null!;
        ThumbnailBlobName = null!;
    }

    [SetsRequiredMembers]
    private AlbumPhoto(
        Guid albumId,
        Guid uploaderId,
        string uploaderName,
        string originalUrl,
        string originalBlobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        string? mediumUrl,
        string? mediumBlobName,
        string? caption,
        long fileSizeBytes,
        int displayOrder,
        int retentionDays,
        AlbumMediaType mediaType,
        long? durationSeconds)
    {
        AlbumId = albumId;
        UploaderId = uploaderId;
        UploaderName = uploaderName;
        OriginalUrl = originalUrl;
        OriginalBlobName = originalBlobName;
        ThumbnailUrl = thumbnailUrl;
        ThumbnailBlobName = thumbnailBlobName;
        MediumUrl = mediumUrl;
        MediumBlobName = mediumBlobName;
        Caption = caption;
        Status = AlbumPhotoStatus.Approved;  // Always approved (no moderation)
        MediaType = mediaType;
        FileSizeBytes = fileSizeBytes;
        DurationSeconds = durationSeconds;
        DisplayOrder = displayOrder;
        UploadedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(retentionDays);
    }

    /// <summary>
    /// Factory method to create a new AlbumPhoto (image media).
    /// Called internally by PhotoAlbum aggregate.
    /// All photos are immediately approved.
    /// </summary>
    internal static AlbumPhoto Create(
        Guid albumId,
        Guid uploaderId,
        string uploaderName,
        string originalUrl,
        string originalBlobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        string mediumUrl,
        string mediumBlobName,
        string? caption,
        long fileSizeBytes,
        int displayOrder,
        int retentionDays)
    {
        if (string.IsNullOrWhiteSpace(uploaderName))
            throw new ArgumentException("Uploader name cannot be empty", nameof(uploaderName));
        if (string.IsNullOrWhiteSpace(originalUrl))
            throw new ArgumentException("Original URL cannot be empty", nameof(originalUrl));
        if (string.IsNullOrWhiteSpace(originalBlobName))
            throw new ArgumentException("Original blob name cannot be empty", nameof(originalBlobName));
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
            throw new ArgumentException("Thumbnail URL cannot be empty", nameof(thumbnailUrl));
        if (string.IsNullOrWhiteSpace(mediumUrl))
            throw new ArgumentException("Medium URL cannot be empty", nameof(mediumUrl));
        if (caption != null && caption.Length > MAX_CAPTION_LENGTH)
            throw new ArgumentException($"Caption cannot exceed {MAX_CAPTION_LENGTH} characters", nameof(caption));
        if (fileSizeBytes <= 0)
            throw new ArgumentException("File size must be greater than 0", nameof(fileSizeBytes));
        if (retentionDays <= 0)
            throw new ArgumentException("Retention days must be greater than 0", nameof(retentionDays));

        return new AlbumPhoto(
            albumId, uploaderId, uploaderName,
            originalUrl, originalBlobName,
            thumbnailUrl, thumbnailBlobName,
            mediumUrl, mediumBlobName,
            caption, fileSizeBytes, displayOrder, retentionDays,
            AlbumMediaType.Photo, durationSeconds: null);
    }

    /// <summary>
    /// Factory method to create a new AlbumPhoto for video media.
    /// Called internally by PhotoAlbum aggregate.
    /// Videos have no medium-size variant — only original + thumbnail.
    /// </summary>
    internal static AlbumPhoto CreateVideo(
        Guid albumId,
        Guid uploaderId,
        string uploaderName,
        string originalUrl,
        string originalBlobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        string? caption,
        long fileSizeBytes,
        int displayOrder,
        int retentionDays,
        long? durationSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(uploaderName))
            throw new ArgumentException("Uploader name cannot be empty", nameof(uploaderName));
        if (string.IsNullOrWhiteSpace(originalUrl))
            throw new ArgumentException("Original URL cannot be empty", nameof(originalUrl));
        if (string.IsNullOrWhiteSpace(originalBlobName))
            throw new ArgumentException("Original blob name cannot be empty", nameof(originalBlobName));
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
            throw new ArgumentException("Thumbnail URL cannot be empty", nameof(thumbnailUrl));
        if (caption != null && caption.Length > MAX_CAPTION_LENGTH)
            throw new ArgumentException($"Caption cannot exceed {MAX_CAPTION_LENGTH} characters", nameof(caption));
        if (fileSizeBytes <= 0)
            throw new ArgumentException("File size must be greater than 0", nameof(fileSizeBytes));
        if (retentionDays <= 0)
            throw new ArgumentException("Retention days must be greater than 0", nameof(retentionDays));

        return new AlbumPhoto(
            albumId, uploaderId, uploaderName,
            originalUrl, originalBlobName,
            thumbnailUrl, thumbnailBlobName,
            mediumUrl: null, mediumBlobName: null,
            caption, fileSizeBytes, displayOrder, retentionDays,
            AlbumMediaType.Video, durationSeconds);
    }

    /// <summary>
    /// Update display order. Internal — only PhotoAlbum aggregate can call this.
    /// </summary>
    internal void UpdateDisplayOrder(int newOrder)
    {
        if (newOrder < 0)
            throw new ArgumentException("Display order must be non-negative", nameof(newOrder));

        DisplayOrder = newOrder;
    }

    /// <summary>
    /// Check if this photo has expired.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
