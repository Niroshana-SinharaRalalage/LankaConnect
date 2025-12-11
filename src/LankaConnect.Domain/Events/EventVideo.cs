using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Represents a video in an event's media gallery
/// Entity within Event aggregate - lifecycle controlled by Event
/// Follows same pattern as EventImage for consistency
/// </summary>
public class EventVideo : BaseEntity
{
    public string VideoUrl { get; private set; }
    public string BlobName { get; private set; }
    public string ThumbnailUrl { get; private set; }
    public string ThumbnailBlobName { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public string Format { get; private set; }
    public long FileSizeBytes { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime UploadedAt { get; private set; }

    // Navigation property to parent Event aggregate
    public Guid EventId { get; private set; }

    // EF Core constructor
    private EventVideo()
    {
        VideoUrl = null!;
        BlobName = null!;
        ThumbnailUrl = null!;
        ThumbnailBlobName = null!;
        Format = null!;
    }

    private EventVideo(
        Guid id,
        Guid eventId,
        string videoUrl,
        string blobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        TimeSpan? duration,
        string format,
        long fileSizeBytes,
        int displayOrder)
    {
        Id = id;
        EventId = eventId;
        VideoUrl = videoUrl;
        BlobName = blobName;
        ThumbnailUrl = thumbnailUrl;
        ThumbnailBlobName = thumbnailBlobName;
        Duration = duration;
        Format = format;
        FileSizeBytes = fileSizeBytes;
        DisplayOrder = displayOrder;
        UploadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new EventVideo
    /// </summary>
    public static EventVideo Create(
        Guid eventId,
        string videoUrl,
        string blobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        TimeSpan? duration,
        string format,
        long fileSizeBytes,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
            throw new ArgumentException("Video URL cannot be empty", nameof(videoUrl));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        if (string.IsNullOrWhiteSpace(thumbnailUrl))
            throw new ArgumentException("Thumbnail URL cannot be empty", nameof(thumbnailUrl));

        if (string.IsNullOrWhiteSpace(thumbnailBlobName))
            throw new ArgumentException("Thumbnail blob name cannot be empty", nameof(thumbnailBlobName));

        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentException("Format cannot be empty", nameof(format));

        if (displayOrder < 1)
            throw new ArgumentException("Display order must be greater than 0", nameof(displayOrder));

        if (fileSizeBytes < 0)
            throw new ArgumentException("File size cannot be negative", nameof(fileSizeBytes));

        return new EventVideo(
            Guid.NewGuid(),
            eventId,
            videoUrl,
            blobName,
            thumbnailUrl,
            thumbnailBlobName,
            duration,
            format,
            fileSizeBytes,
            displayOrder);
    }

    /// <summary>
    /// Updates the display order of this video
    /// Internal method - only Event aggregate can call this
    /// </summary>
    internal void UpdateDisplayOrder(int newOrder)
    {
        if (newOrder < 1)
            throw new ArgumentException("Display order must be greater than 0", nameof(newOrder));

        DisplayOrder = newOrder;
    }
}
