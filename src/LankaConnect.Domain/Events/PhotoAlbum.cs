using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Aggregate root for post-event photo albums.
/// Separate from Event aggregate to maintain DDD boundaries:
/// - Event images (max 10) are marketing/listing photos managed by organizer only
/// - Album photos (max 200) are post-event memories managed by organizer
///
/// Lifecycle: Draft → Published
/// Multiple albums per event allowed (unique constraint on EventId + Name).
/// Photos auto-expire after RetentionDays (default 7 days).
/// </summary>
public class PhotoAlbum : BaseEntity
{
    private readonly List<AlbumPhoto> _photos = new();

    public const int MAX_PHOTOS = 200;
    public const int DEFAULT_RETENTION_DAYS = 7;
    public const int MAX_DESCRIPTION_LENGTH = 2000;
    public const int MAX_NAME_LENGTH = 100;

    public Guid EventId { get; private set; }
    public Guid OrganizerId { get; private set; }
    public string EventTitle { get; private set; }           // Denormalized for email notifications
    public string Name { get; private set; }                  // Album display name (required)
    public AlbumStatus Status { get; private set; }
    public string? Description { get; private set; }
    public string? CoverPhotoUrl { get; private set; }
    public int RetentionDays { get; private set; }
    public int PhotoCount { get; private set; }              // Cached count
    public DateTime? PublishedAt { get; private set; }

    public IReadOnlyList<AlbumPhoto> Photos => _photos.AsReadOnly();

    // EF Core constructor
    private PhotoAlbum()
    {
        EventTitle = null!;
        Name = null!;
    }

    private PhotoAlbum(
        Guid eventId,
        Guid organizerId,
        string eventTitle,
        string name,
        string? description)
    {
        EventId = eventId;
        OrganizerId = organizerId;
        EventTitle = eventTitle;
        Name = name;
        Description = description;
        Status = AlbumStatus.Draft;
        RetentionDays = DEFAULT_RETENTION_DAYS;
        PhotoCount = 0;
    }

    /// <summary>
    /// Factory method to create a new PhotoAlbum for an event.
    /// Multiple albums per event allowed — unique constraint on (EventId, Name).
    /// </summary>
    public static Result<PhotoAlbum> Create(
        Guid eventId,
        Guid organizerId,
        string eventTitle,
        string name,
        string? description = null)
    {
        if (eventId == Guid.Empty)
            return Result<PhotoAlbum>.Failure("Event ID is required");
        if (organizerId == Guid.Empty)
            return Result<PhotoAlbum>.Failure("Organizer ID is required");
        if (string.IsNullOrWhiteSpace(eventTitle))
            return Result<PhotoAlbum>.Failure("Event title is required");
        if (string.IsNullOrWhiteSpace(name))
            return Result<PhotoAlbum>.Failure("Album name is required");
        if (name.Length > MAX_NAME_LENGTH)
            return Result<PhotoAlbum>.Failure($"Album name cannot exceed {MAX_NAME_LENGTH} characters");
        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result<PhotoAlbum>.Failure($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        var album = new PhotoAlbum(eventId, organizerId, eventTitle, name, description);

        return Result<PhotoAlbum>.Success(album);
    }

    /// <summary>
    /// Publish the album — makes it visible to attendees on the event details page.
    /// Requires at least one photo to be uploaded first.
    /// Note: Email notification is sent separately via SendAlbumNotificationCommand.
    /// </summary>
    public Result Publish()
    {
        if (Status == AlbumStatus.Published)
            return Result.Failure("Album is already published");

        if (PhotoCount == 0)
            return Result.Failure("Upload at least one photo before publishing");

        Status = AlbumStatus.Published;
        PublishedAt = DateTime.UtcNow;
        MarkAsUpdated();

        RaiseDomainEvent(new PhotoAlbumPublishedDomainEvent(Id, EventId, EventTitle, Name));

        return Result.Success();
    }

    /// <summary>
    /// Update album details (name and description).
    /// </summary>
    public Result UpdateDetails(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Album name is required");
        if (name.Length > MAX_NAME_LENGTH)
            return Result.Failure($"Album name cannot exceed {MAX_NAME_LENGTH} characters");
        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result.Failure($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        Name = name;
        if (description != null)
            Description = description;

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Add a photo to the album.
    /// Photos can be added in both Draft and Published states.
    /// All photos are immediately approved (no moderation).
    /// </summary>
    public Result<AlbumPhoto> AddPhoto(
        Guid uploaderId,
        string uploaderName,
        string originalUrl,
        string originalBlobName,
        string thumbnailUrl,
        string thumbnailBlobName,
        string mediumUrl,
        string mediumBlobName,
        string? caption,
        long fileSizeBytes)
    {
        if (_photos.Count >= MAX_PHOTOS)
            return Result<AlbumPhoto>.Failure($"Album has reached the maximum of {MAX_PHOTOS} photos");

        var displayOrder = _photos.Count + 1;

        try
        {
            var photo = AlbumPhoto.Create(
                Id, uploaderId, uploaderName,
                originalUrl, originalBlobName,
                thumbnailUrl, thumbnailBlobName,
                mediumUrl, mediumBlobName,
                caption, fileSizeBytes, displayOrder, RetentionDays);

            _photos.Add(photo);
            PhotoCount++;

            MarkAsUpdated();
            RaiseDomainEvent(new PhotoUploadedToAlbumDomainEvent(Id, photo.Id, uploaderId));

            return Result<AlbumPhoto>.Success(photo);
        }
        catch (ArgumentException ex)
        {
            return Result<AlbumPhoto>.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Remove a photo from the album.
    /// Returns the removed photo so caller can clean up blob storage.
    /// </summary>
    public Result<AlbumPhoto> RemovePhoto(Guid photoId, Guid requesterId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null)
            return Result<AlbumPhoto>.Failure($"Photo with ID {photoId} not found in this album");

        // Only photo uploader or album organizer can delete
        if (photo.UploaderId != requesterId && OrganizerId != requesterId)
            return Result<AlbumPhoto>.Failure("Only the photo uploader or event organizer can delete this photo");

        _photos.Remove(photo);
        PhotoCount = Math.Max(0, PhotoCount - 1);

        MarkAsUpdated();
        return Result<AlbumPhoto>.Success(photo);
    }

    /// <summary>
    /// Set a photo as the album cover.
    /// </summary>
    public Result SetCoverPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null)
            return Result.Failure($"Photo with ID {photoId} not found in this album");

        CoverPhotoUrl = photo.MediumUrl;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Decrement photo count when expired photos are cleaned up by background service.
    /// </summary>
    public void DecrementPhotoCount(int count = 1)
    {
        PhotoCount = Math.Max(0, PhotoCount - count);
        MarkAsUpdated();
    }
}
