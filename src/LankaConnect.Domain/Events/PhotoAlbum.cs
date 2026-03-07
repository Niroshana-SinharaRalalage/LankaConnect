using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Aggregate root for post-event photo albums.
/// Separate from Event aggregate to maintain DDD boundaries:
/// - Event images (max 10) are marketing/listing photos managed by organizer only
/// - Album photos (max 200) are post-event memories, potentially community-contributed
///
/// Lifecycle: Draft → Published → Closed
/// Photos auto-expire after RetentionDays (default 7 days).
/// </summary>
public class PhotoAlbum : BaseEntity
{
    private readonly List<AlbumPhoto> _photos = new();

    public const int MAX_PHOTOS = 200;
    public const int DEFAULT_RETENTION_DAYS = 7;
    public const int MAX_DESCRIPTION_LENGTH = 2000;

    public Guid EventId { get; private set; }
    public Guid OrganizerId { get; private set; }
    public string EventTitle { get; private set; }           // Denormalized for display
    public AlbumStatus Status { get; private set; }
    public AlbumUploadPermission UploadPermission { get; private set; }
    public AlbumModerationMode ModerationMode { get; private set; }
    public string? Description { get; private set; }
    public string? CoverPhotoUrl { get; private set; }
    public int RetentionDays { get; private set; }
    public int PhotoCount { get; private set; }              // Cached count (approved only)
    public DateTime? PublishedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    public IReadOnlyList<AlbumPhoto> Photos => _photos.AsReadOnly();

    // EF Core constructor
    private PhotoAlbum()
    {
        EventTitle = null!;
    }

    private PhotoAlbum(
        Guid eventId,
        Guid organizerId,
        string eventTitle,
        string? description)
    {
        EventId = eventId;
        OrganizerId = organizerId;
        EventTitle = eventTitle;
        Description = description;
        Status = AlbumStatus.Draft;
        UploadPermission = AlbumUploadPermission.OrganizerOnly;
        ModerationMode = AlbumModerationMode.PostModeration;
        RetentionDays = DEFAULT_RETENTION_DAYS;
        PhotoCount = 0;
    }

    /// <summary>
    /// Factory method to create a new PhotoAlbum for an event.
    /// Only one album per event (enforced by unique constraint on EventId).
    /// </summary>
    public static Result<PhotoAlbum> Create(
        Guid eventId,
        Guid organizerId,
        string eventTitle,
        string? description = null)
    {
        if (eventId == Guid.Empty)
            return Result<PhotoAlbum>.Failure("Event ID is required");
        if (organizerId == Guid.Empty)
            return Result<PhotoAlbum>.Failure("Organizer ID is required");
        if (string.IsNullOrWhiteSpace(eventTitle))
            return Result<PhotoAlbum>.Failure("Event title is required");
        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result<PhotoAlbum>.Failure($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        var album = new PhotoAlbum(eventId, organizerId, eventTitle, description);
        album.RaiseDomainEvent(new PhotoAlbumCreatedDomainEvent(album.Id, eventId));

        return Result<PhotoAlbum>.Success(album);
    }

    /// <summary>
    /// Publish the album — makes it visible to attendees and triggers notification email.
    /// </summary>
    public Result Publish()
    {
        if (Status == AlbumStatus.Published)
            return Result.Failure("Album is already published");
        if (Status == AlbumStatus.Closed)
            return Result.Failure("Cannot publish a closed album");

        Status = AlbumStatus.Published;
        PublishedAt = DateTime.UtcNow;
        MarkAsUpdated();

        RaiseDomainEvent(new PhotoAlbumPublishedDomainEvent(Id, EventId, EventTitle));

        return Result.Success();
    }

    /// <summary>
    /// Close the album — stop accepting new uploads. Photos remain viewable until expiry.
    /// </summary>
    public Result Close()
    {
        if (Status == AlbumStatus.Closed)
            return Result.Failure("Album is already closed");
        if (Status == AlbumStatus.Draft)
            return Result.Failure("Cannot close a draft album — publish it first");

        Status = AlbumStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Update album settings (permissions, moderation, description).
    /// Only allowed while album is in Draft or Published status.
    /// </summary>
    public Result UpdateSettings(
        AlbumUploadPermission? uploadPermission = null,
        AlbumModerationMode? moderationMode = null,
        string? description = null)
    {
        if (Status == AlbumStatus.Closed)
            return Result.Failure("Cannot update settings on a closed album");

        if (description != null && description.Length > MAX_DESCRIPTION_LENGTH)
            return Result.Failure($"Description cannot exceed {MAX_DESCRIPTION_LENGTH} characters");

        if (uploadPermission.HasValue)
            UploadPermission = uploadPermission.Value;
        if (moderationMode.HasValue)
            ModerationMode = moderationMode.Value;
        if (description != null)
            Description = description;

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Add a photo to the album.
    /// Validates album status, photo count limits, and applies moderation rules.
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
        if (Status == AlbumStatus.Draft)
            return Result<AlbumPhoto>.Failure("Album is not yet published — cannot accept uploads");
        if (Status == AlbumStatus.Closed)
            return Result<AlbumPhoto>.Failure("Album is closed — no more uploads accepted");

        // Count non-rejected photos toward the limit
        var activePhotoCount = _photos.Count(p => p.Status != AlbumPhotoStatus.Rejected);
        if (activePhotoCount >= MAX_PHOTOS)
            return Result<AlbumPhoto>.Failure($"Album has reached the maximum of {MAX_PHOTOS} photos");

        // Determine initial status based on moderation mode
        var initialStatus = ModerationMode == AlbumModerationMode.PreApproval
            ? AlbumPhotoStatus.PendingApproval
            : AlbumPhotoStatus.Approved;

        var displayOrder = _photos.Count + 1;

        try
        {
            var photo = AlbumPhoto.Create(
                Id, uploaderId, uploaderName,
                originalUrl, originalBlobName,
                thumbnailUrl, thumbnailBlobName,
                mediumUrl, mediumBlobName,
                caption, initialStatus, fileSizeBytes, displayOrder, RetentionDays);

            _photos.Add(photo);

            // Only increment count for immediately-approved photos
            if (initialStatus == AlbumPhotoStatus.Approved)
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

        if (photo.Status == AlbumPhotoStatus.Approved)
            PhotoCount = Math.Max(0, PhotoCount - 1);

        MarkAsUpdated();
        return Result<AlbumPhoto>.Success(photo);
    }

    /// <summary>
    /// Approve a pending photo (organizer action).
    /// </summary>
    public Result ApprovePhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null)
            return Result.Failure($"Photo with ID {photoId} not found in this album");

        if (photo.Status != AlbumPhotoStatus.PendingApproval)
            return Result.Failure($"Photo is not pending approval (current status: {photo.Status})");

        photo.Approve();
        PhotoCount++;
        MarkAsUpdated();

        RaiseDomainEvent(new PhotoApprovedDomainEvent(Id, photoId));
        return Result.Success();
    }

    /// <summary>
    /// Reject a pending photo (organizer action).
    /// Returns the rejected photo so caller can clean up blob storage.
    /// </summary>
    public Result<AlbumPhoto> RejectPhoto(Guid photoId)
    {
        var photo = _photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null)
            return Result<AlbumPhoto>.Failure($"Photo with ID {photoId} not found in this album");

        if (photo.Status != AlbumPhotoStatus.PendingApproval)
            return Result<AlbumPhoto>.Failure($"Photo is not pending approval (current status: {photo.Status})");

        photo.Reject();
        MarkAsUpdated();

        RaiseDomainEvent(new PhotoRejectedDomainEvent(Id, photoId));
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

        if (photo.Status != AlbumPhotoStatus.Approved)
            return Result.Failure("Only approved photos can be set as album cover");

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
