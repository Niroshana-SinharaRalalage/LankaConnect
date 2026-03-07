using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Represents a photo in an event's post-event photo album.
/// Entity within PhotoAlbum aggregate — lifecycle controlled by PhotoAlbum.
/// Stores 3 image sizes: original, medium (800px), thumbnail (150px).
/// Photos auto-expire after RetentionDays (default 7 days).
/// </summary>
public class AlbumPhoto : BaseEntity
{
    public Guid AlbumId { get; private set; }
    public Guid UploaderId { get; private set; }
    public string UploaderName { get; private set; }        // Denormalized for display without user lookup
    public string OriginalUrl { get; private set; }          // Full-size EXIF-stripped image
    public string OriginalBlobName { get; private set; }
    public string ThumbnailUrl { get; private set; }         // 150x150 center-crop WebP
    public string ThumbnailBlobName { get; private set; }
    public string MediumUrl { get; private set; }            // 800px wide WebP
    public string MediumBlobName { get; private set; }
    public string? Caption { get; private set; }             // Optional caption (max 500 chars)
    public AlbumPhotoStatus Status { get; private set; }
    public long FileSizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }          // Auto-deletion date
    public int DisplayOrder { get; private set; }

    public const int MAX_CAPTION_LENGTH = 500;

    // EF Core constructor
    private AlbumPhoto()
    {
        UploaderName = null!;
        OriginalUrl = null!;
        OriginalBlobName = null!;
        ThumbnailUrl = null!;
        ThumbnailBlobName = null!;
        MediumUrl = null!;
        MediumBlobName = null!;
    }

    private AlbumPhoto(
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
        AlbumPhotoStatus status,
        long fileSizeBytes,
        int displayOrder,
        int retentionDays)
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
        Status = status;
        FileSizeBytes = fileSizeBytes;
        DisplayOrder = displayOrder;
        UploadedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(retentionDays);
    }

    /// <summary>
    /// Factory method to create a new AlbumPhoto.
    /// Called internally by PhotoAlbum aggregate.
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
        AlbumPhotoStatus status,
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
            caption, status, fileSizeBytes, displayOrder, retentionDays);
    }

    /// <summary>
    /// Approve this photo (organizer action).
    /// Internal — only PhotoAlbum aggregate can call this.
    /// </summary>
    internal void Approve()
    {
        if (Status != AlbumPhotoStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve photo in status {Status}");

        Status = AlbumPhotoStatus.Approved;
        MarkAsUpdated();
    }

    /// <summary>
    /// Reject this photo (organizer action).
    /// Internal — only PhotoAlbum aggregate can call this.
    /// </summary>
    internal void Reject()
    {
        if (Status != AlbumPhotoStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject photo in status {Status}");

        Status = AlbumPhotoStatus.Rejected;
        MarkAsUpdated();
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
