using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Represents an image in an event's gallery
/// Entity within Event aggregate - lifecycle controlled by Event
/// </summary>
public class EventImage : BaseEntity
{
    public string ImageUrl { get; private set; }
    public string BlobName { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; } // Phase 6A.13: Explicit primary/main image flag
    public DateTime UploadedAt { get; private set; }

    // Navigation property to parent Event aggregate
    public Guid EventId { get; private set; }

    // EF Core constructor
    private EventImage()
    {
        ImageUrl = null!;
        BlobName = null!;
    }

    private EventImage(Guid id, Guid eventId, string imageUrl, string blobName, int displayOrder, bool isPrimary = false)
    {
        Id = id;
        EventId = eventId;
        ImageUrl = imageUrl;
        BlobName = blobName;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        UploadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new EventImage
    /// </summary>
    public static EventImage Create(Guid eventId, string imageUrl, string blobName, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL cannot be empty", nameof(imageUrl));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        if (displayOrder < 1)
            throw new ArgumentException("Display order must be greater than 0", nameof(displayOrder));

        return new EventImage(Guid.NewGuid(), eventId, imageUrl, blobName, displayOrder);
    }

    /// <summary>
    /// Factory method to create an EventImage with a specific ID
    /// Used internally by Event aggregate for replace operations
    /// </summary>
    internal static EventImage CreateWithId(Guid id, Guid eventId, string imageUrl, string blobName, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            throw new ArgumentException("Image URL cannot be empty", nameof(imageUrl));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be empty", nameof(blobName));

        if (displayOrder < 1)
            throw new ArgumentException("Display order must be greater than 0", nameof(displayOrder));

        return new EventImage(id, eventId, imageUrl, blobName, displayOrder);
    }

    /// <summary>
    /// Updates the display order of this image
    /// Internal method - only Event aggregate can call this
    /// </summary>
    internal void UpdateDisplayOrder(int newOrder)
    {
        if (newOrder < 1)
            throw new ArgumentException("Display order must be greater than 0", nameof(newOrder));

        DisplayOrder = newOrder;
    }

    /// <summary>
    /// Marks this image as the primary/main image for the event
    /// Internal method - only Event aggregate can call this
    /// Phase 6A.13: Primary image selection feature
    /// </summary>
    internal void SetAsPrimary()
    {
        IsPrimary = true;
    }

    /// <summary>
    /// Unmarks this image as primary
    /// Internal method - only Event aggregate can call this
    /// Phase 6A.13: Primary image selection feature
    /// </summary>
    internal void UnmarkAsPrimary()
    {
        IsPrimary = false;
    }
}
