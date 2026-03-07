using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Events.Commands.PhotoAlbums.CreatePhotoAlbum;
using LankaConnect.Application.Events.Commands.PhotoAlbums.UpdateAlbumSettings;
using LankaConnect.Application.Events.Commands.PhotoAlbums.PublishPhotoAlbum;
using LankaConnect.Application.Events.Commands.PhotoAlbums.ClosePhotoAlbum;
using LankaConnect.Application.Events.Commands.PhotoAlbums.UploadAlbumPhoto;
using LankaConnect.Application.Events.Commands.PhotoAlbums.DeleteAlbumPhoto;
using LankaConnect.Application.Events.Commands.PhotoAlbums.ApproveAlbumPhoto;
using LankaConnect.Application.Events.Commands.PhotoAlbums.RejectAlbumPhoto;
using LankaConnect.Application.Events.Commands.PhotoAlbums.SetAlbumCoverPhoto;
using LankaConnect.Application.Events.Queries.PhotoAlbums.GetAlbumByEventId;
using LankaConnect.Application.Events.Queries.PhotoAlbums.GetAlbumPhotos;
using LankaConnect.Application.Events.Queries.PhotoAlbums.GetPendingPhotos;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Manages photo albums for events. Provides endpoints for album lifecycle
/// (create, publish, close), settings management, and photo operations
/// (upload, delete, approve, reject, set cover).
/// </summary>
[Route("api/events/{eventId:guid}/album")]
public class PhotoAlbumsController : BaseController<PhotoAlbumsController>
{
    public PhotoAlbumsController(IMediator mediator, ILogger<PhotoAlbumsController> logger)
        : base(mediator, logger)
    {
    }

    // ==================== ALBUM LIFECYCLE ====================

    /// <summary>
    /// Create a photo album for an event.
    /// Only one album per event. Event must be in Active, Completed, or Archived status.
    /// </summary>
    /// <param name="eventId">The event ID to create the album for.</param>
    /// <param name="request">Optional album creation settings.</param>
    /// <returns>The created photo album.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PhotoAlbumDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePhotoAlbum(Guid eventId, [FromBody] CreatePhotoAlbumRequest? request = null)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Creating photo album for event {EventId} by user {UserId}",
            eventId, userId);

        var command = new CreatePhotoAlbumCommand(
            EventId: eventId,
            UserId: userId,
            Description: request?.Description);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get the photo album for an event.
    /// Returns the album metadata including status, settings, and photo count.
    /// Returns null wrapped in success if no album exists.
    /// </summary>
    /// <param name="eventId">The event ID to get the album for.</param>
    /// <returns>The photo album DTO, or null if no album exists.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PhotoAlbumDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAlbumByEventId(Guid eventId)
    {
        Logger.LogInformation("Getting photo album for event {EventId}", eventId);

        var query = new GetAlbumByEventIdQuery(EventId: eventId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Update album settings (upload permission, moderation mode, description).
    /// Only the event organizer can update settings.
    /// Only allowed while album is in Draft or Published status.
    /// </summary>
    /// <param name="eventId">The event ID whose album settings to update.</param>
    /// <param name="request">The settings to update. All fields are optional.</param>
    [HttpPut("settings")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAlbumSettings(Guid eventId, [FromBody] UpdateAlbumSettingsRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Updating album settings for event {EventId} by user {UserId}, UploadPermission={UploadPermission}, ModerationMode={ModerationMode}",
            eventId, userId, request.UploadPermission, request.ModerationMode);

        var command = new UpdateAlbumSettingsCommand(
            EventId: eventId,
            UserId: userId,
            UploadPermission: request.UploadPermission,
            ModerationMode: request.ModerationMode,
            Description: request.Description);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Publish a photo album, making it visible to attendees.
    /// Only the event organizer can publish an album.
    /// </summary>
    /// <param name="eventId">The event ID whose album to publish.</param>
    [HttpPost("publish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishPhotoAlbum(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Publishing photo album for event {EventId} by user {UserId}",
            eventId, userId);

        var command = new PublishPhotoAlbumCommand(
            EventId: eventId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Close a photo album, stopping new uploads.
    /// Photos remain viewable until their individual expiry dates.
    /// Only the event organizer can close an album.
    /// </summary>
    /// <param name="eventId">The event ID whose album to close.</param>
    [HttpPost("close")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClosePhotoAlbum(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Closing photo album for event {EventId} by user {UserId}",
            eventId, userId);

        var command = new ClosePhotoAlbumCommand(
            EventId: eventId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== PHOTO OPERATIONS ====================

    /// <summary>
    /// Upload a photo to an event's photo album.
    /// Validates upload permissions, processes the image (EXIF strip, thumbnails),
    /// and uploads to Azure Blob Storage.
    /// </summary>
    /// <param name="eventId">The event ID whose album to upload to.</param>
    /// <param name="image">The image file to upload.</param>
    /// <param name="caption">Optional caption for the photo.</param>
    /// <returns>The uploaded photo metadata.</returns>
    [HttpPost("photos")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AlbumPhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAlbumPhoto(Guid eventId, IFormFile image, [FromForm] string? caption = null)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Image file is required");

        var userId = User.GetUserId();
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        // Read image data into memory
        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);
        var imageData = memoryStream.ToArray();

        Logger.LogInformation(
            "Uploading photo to album for event {EventId} by user {UserId} ({UserName}), FileName={FileName}, Size={Size}",
            eventId, userId, userName, image.FileName, imageData.Length);

        var command = new UploadAlbumPhotoCommand(
            EventId: eventId,
            UploaderId: userId,
            UploaderName: userName,
            ImageData: imageData,
            FileName: image.FileName,
            Caption: caption);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated approved photos from an album.
    /// Uses cursor-based pagination for infinite scroll support.
    /// </summary>
    /// <param name="eventId">The event ID whose album photos to retrieve.</param>
    /// <param name="pageSize">Number of photos per page (default 20, max 50).</param>
    /// <param name="cursor">Cursor for pagination (UploadedAt of last photo in previous page).</param>
    /// <returns>Paginated list of approved album photos.</returns>
    [HttpGet("photos")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedAlbumPhotosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAlbumPhotos(
        Guid eventId,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? cursor = null)
    {
        Logger.LogInformation(
            "Getting album photos for event {EventId}, PageSize={PageSize}, Cursor={Cursor}",
            eventId, pageSize, cursor);

        var query = new GetAlbumPhotosQuery(
            EventId: eventId,
            PageSize: pageSize,
            Cursor: cursor);

        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get all pending photos for organizer moderation queue.
    /// Only the event organizer can view pending photos.
    /// </summary>
    /// <param name="eventId">The event ID whose pending photos to retrieve.</param>
    /// <returns>List of photos awaiting approval.</returns>
    [HttpGet("photos/pending")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<AlbumPhotoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPendingPhotos(Guid eventId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Getting pending photos for event {EventId} by organizer {UserId}",
            eventId, userId);

        var query = new GetPendingPhotosQuery(
            EventId: eventId,
            UserId: userId);

        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a photo from an event's photo album.
    /// Only the photo uploader or event organizer can delete a photo.
    /// Removes blobs from Azure Blob Storage.
    /// </summary>
    /// <param name="eventId">The event ID whose album contains the photo.</param>
    /// <param name="photoId">The ID of the photo to delete.</param>
    [HttpDelete("photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAlbumPhoto(Guid eventId, Guid photoId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Deleting photo {PhotoId} from album for event {EventId} by user {UserId}",
            photoId, eventId, userId);

        var command = new DeleteAlbumPhotoCommand(
            EventId: eventId,
            PhotoId: photoId,
            RequesterId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Approve a pending photo in a pre-moderation album.
    /// Only the event organizer can approve photos.
    /// </summary>
    /// <param name="eventId">The event ID whose album contains the photo.</param>
    /// <param name="photoId">The ID of the photo to approve.</param>
    [HttpPost("photos/{photoId:guid}/approve")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApproveAlbumPhoto(Guid eventId, Guid photoId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Approving photo {PhotoId} in album for event {EventId} by user {UserId}",
            photoId, eventId, userId);

        var command = new ApproveAlbumPhotoCommand(
            EventId: eventId,
            PhotoId: photoId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reject a pending photo in a pre-moderation album.
    /// Only the event organizer can reject photos.
    /// Rejected photos have their blobs deleted from Azure Blob Storage.
    /// </summary>
    /// <param name="eventId">The event ID whose album contains the photo.</param>
    /// <param name="photoId">The ID of the photo to reject.</param>
    [HttpPost("photos/{photoId:guid}/reject")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RejectAlbumPhoto(Guid eventId, Guid photoId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Rejecting photo {PhotoId} in album for event {EventId} by user {UserId}",
            photoId, eventId, userId);

        var command = new RejectAlbumPhotoCommand(
            EventId: eventId,
            PhotoId: photoId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Set an approved photo as the album cover.
    /// Only the event organizer can set the cover photo.
    /// </summary>
    /// <param name="eventId">The event ID whose album cover to set.</param>
    /// <param name="photoId">The ID of the approved photo to use as cover.</param>
    [HttpPut("cover/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetAlbumCoverPhoto(Guid eventId, Guid photoId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Setting cover photo {PhotoId} for album on event {EventId} by user {UserId}",
            photoId, eventId, userId);

        var command = new SetAlbumCoverPhotoCommand(
            EventId: eventId,
            PhotoId: photoId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}

// ==================== REQUEST DTOs ====================

/// <summary>
/// Request body for creating a photo album.
/// </summary>
public class CreatePhotoAlbumRequest
{
    /// <summary>
    /// Optional description for the photo album.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request body for updating album settings.
/// All fields are optional; only provided values will be updated.
/// </summary>
public class UpdateAlbumSettingsRequest
{
    /// <summary>
    /// Who can upload photos: OrganizerOnly, RegisteredAttendees, or AnyAuthenticated.
    /// </summary>
    public AlbumUploadPermission? UploadPermission { get; set; }

    /// <summary>
    /// Moderation mode: None (auto-approve) or PreApproval (organizer must approve).
    /// </summary>
    public AlbumModerationMode? ModerationMode { get; set; }

    /// <summary>
    /// Optional description for the photo album.
    /// </summary>
    public string? Description { get; set; }
}
