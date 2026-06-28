using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.API.Extensions;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.CreatePhotoAlbum;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.UpdateAlbumDetails;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.PublishPhotoAlbum;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.UploadAlbumPhoto;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.DeleteAlbumPhoto;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.BulkDeleteAlbumPhotos;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.SetAlbumCoverPhoto;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.DeletePhotoAlbum;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.SendAlbumNotification;
using LankaConnect.Products.LankaEvents.Application.Commands.PhotoAlbums.UploadAlbumVideo;
using LankaConnect.Application.Events.Queries.PhotoAlbums.GetAlbumByEventId;
using LankaConnect.Application.Events.Queries.PhotoAlbums.GetAlbumPhotos;
using LankaConnect.Application.Events.Queries.PhotoAlbums.DownloadAlbumZip;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Manages photo albums for events. Provides endpoints for album lifecycle
/// (create, publish), details management, and photo operations
/// (upload, delete, set cover).
/// </summary>
[Route("api/events/{eventId:guid}/albums")]
public class PhotoAlbumsController : BaseController<PhotoAlbumsController>
{
    public PhotoAlbumsController(IMediator mediator, ILogger<PhotoAlbumsController> logger)
        : base(mediator, logger)
    {
    }

    // ==================== ALBUM LIFECYCLE ====================

    /// <summary>
    /// Create a photo album for an event.
    /// Multiple albums per event allowed (unique constraint on EventId + Name).
    /// Event must be in Published, Active, Completed, or Archived status.
    /// </summary>
    /// <param name="eventId">The event ID to create the album for.</param>
    /// <param name="request">Album creation details including name.</param>
    /// <returns>The created photo album.</returns>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(PhotoAlbumDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePhotoAlbum(Guid eventId, [FromBody] CreatePhotoAlbumRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Creating photo album for event {EventId} by user {UserId}, Name={AlbumName}",
            eventId, userId, request.Name);

        var command = new CreatePhotoAlbumCommand(
            EventId: eventId,
            UserId: userId,
            Name: request.Name,
            Description: request.Description);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get all photo albums for an event.
    /// Returns album metadata including status, settings, and photo count.
    /// Returns empty list if no albums exist.
    /// </summary>
    /// <param name="eventId">The event ID to get albums for.</param>
    /// <returns>List of photo album DTOs.</returns>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PhotoAlbumDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAlbumsByEventId(Guid eventId)
    {
        Logger.LogInformation("Getting photo albums for event {EventId}", eventId);

        var query = new GetAlbumByEventIdQuery(EventId: eventId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Update album details (name and description).
    /// Only the event organizer can update details.
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID to update.</param>
    /// <param name="request">The details to update.</param>
    [HttpPut("{albumId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAlbumDetails(Guid eventId, Guid albumId, [FromBody] UpdateAlbumDetailsRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Updating album details for album {AlbumId} on event {EventId} by user {UserId}, Name={AlbumName}",
            albumId, eventId, userId, request.Name);

        var command = new UpdateAlbumDetailsCommand(
            AlbumId: albumId,
            UserId: userId,
            Name: request.Name,
            Description: request.Description);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Publish a photo album, making it visible to attendees.
    /// Only the event organizer can publish an album.
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID to publish.</param>
    [HttpPost("{albumId:guid}/publish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishPhotoAlbum(Guid eventId, Guid albumId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Publishing photo album {AlbumId} for event {EventId} by user {UserId}",
            albumId, eventId, userId);

        var command = new PublishPhotoAlbumCommand(
            AlbumId: albumId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a draft photo album and clean up all associated blob storage.
    /// Only draft albums can be deleted. Published albums cannot be removed.
    /// Only the event organizer can delete albums.
    /// </summary>
    [HttpDelete("{albumId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeletePhotoAlbum(Guid eventId, Guid albumId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Deleting photo album {AlbumId} for event {EventId} by user {UserId}",
            albumId, eventId, userId);

        var command = new DeletePhotoAlbumCommand(
            AlbumId: albumId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Send email notification to registered attendees about a published album.
    /// Separate from publishing: organizer explicitly triggers notification.
    /// Album must be in Published status.
    /// </summary>
    [HttpPost("{albumId:guid}/notify")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendAlbumNotification(Guid eventId, Guid albumId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Sending album notification for album {AlbumId}, event {EventId} by user {UserId}",
            albumId, eventId, userId);

        var command = new SendAlbumNotificationCommand(
            EventId: eventId,
            AlbumId: albumId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== PHOTO OPERATIONS ====================

    /// <summary>
    /// Upload a photo to a photo album.
    /// Only the event organizer can upload photos.
    /// Processes the image (EXIF strip, thumbnails) and uploads to Azure Blob Storage.
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID to upload to.</param>
    /// <param name="image">The image file to upload.</param>
    /// <param name="caption">Optional caption for the photo.</param>
    /// <returns>The uploaded photo metadata.</returns>
    [HttpPost("{albumId:guid}/photos")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AlbumPhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAlbumPhoto(Guid eventId, Guid albumId, IFormFile image, [FromForm] string? caption = null)
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
            "Uploading photo to album {AlbumId} for event {EventId} by user {UserId} ({UserName}), FileName={FileName}, Size={Size}",
            albumId, eventId, userId, userName, image.FileName, imageData.Length);

        var command = new UploadAlbumPhotoCommand(
            AlbumId: albumId,
            UploaderId: userId,
            UploaderName: userName,
            ImageData: imageData,
            FileName: image.FileName,
            Caption: caption);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Upload a video to a photo album.
    /// Requires a video file and a thumbnail image. Video is stored as-is (no transcoding).
    /// Thumbnail is processed (EXIF stripped, resized to 150x150 WebP).
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID to upload to.</param>
    /// <param name="video">The video file to upload (MP4, WebM, or MOV, max 500 MB).</param>
    /// <param name="thumbnail">A thumbnail image for the video (JPEG, PNG, GIF, or WebP, max 10 MB).</param>
    /// <param name="caption">Optional caption for the video.</param>
    /// <param name="durationSeconds">Optional video duration in seconds.</param>
    /// <returns>The uploaded media metadata.</returns>
    [HttpPost("{albumId:guid}/videos")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AlbumPhotoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [RequestSizeLimit(500L * 1024 * 1024)] // 500 MB for video uploads
    public async Task<IActionResult> UploadAlbumVideo(
        Guid eventId, Guid albumId,
        IFormFile video, IFormFile thumbnail,
        [FromForm] string? caption = null,
        [FromForm] long? durationSeconds = null)
    {
        if (video == null || video.Length == 0)
            return BadRequest("Video file is required");

        if (thumbnail == null || thumbnail.Length == 0)
            return BadRequest("Thumbnail image is required");

        var userId = User.GetUserId();
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        // Read video data into memory
        using var videoStream = new MemoryStream();
        await video.CopyToAsync(videoStream);
        var videoData = videoStream.ToArray();

        // Read thumbnail data into memory
        using var thumbStream = new MemoryStream();
        await thumbnail.CopyToAsync(thumbStream);
        var thumbnailData = thumbStream.ToArray();

        Logger.LogInformation(
            "Uploading video to album {AlbumId} for event {EventId} by user {UserId} ({UserName}), FileName={FileName}, VideoSize={VideoSize}, ThumbSize={ThumbSize}",
            albumId, eventId, userId, userName, video.FileName, videoData.Length, thumbnailData.Length);

        var command = new UploadAlbumVideoCommand(
            AlbumId: albumId,
            UploaderId: userId,
            UploaderName: userName,
            VideoData: videoData,
            VideoFileName: video.FileName,
            ThumbnailData: thumbnailData,
            ThumbnailFileName: thumbnail.FileName,
            Caption: caption,
            DurationSeconds: durationSeconds);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get paginated approved photos from an album.
    /// Uses cursor-based pagination for infinite scroll support.
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID whose photos to retrieve.</param>
    /// <param name="pageSize">Number of photos per page (default 20, max 50).</param>
    /// <param name="cursor">Cursor for pagination (UploadedAt of last photo in previous page).</param>
    /// <returns>Paginated list of approved album photos.</returns>
    [HttpGet("{albumId:guid}/photos")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedAlbumPhotosResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAlbumPhotos(
        Guid eventId,
        Guid albumId,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? cursor = null)
    {
        Logger.LogInformation(
            "Getting album photos for album {AlbumId} on event {EventId}, PageSize={PageSize}, Cursor={Cursor}",
            albumId, eventId, pageSize, cursor);

        var query = new GetAlbumPhotosQuery(
            AlbumId: albumId,
            PageSize: pageSize,
            Cursor: cursor);

        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a photo from a photo album.
    /// Only the photo uploader or event organizer can delete a photo.
    /// Removes blobs from Azure Blob Storage.
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID containing the photo.</param>
    /// <param name="photoId">The ID of the photo to delete.</param>
    [HttpDelete("{albumId:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAlbumPhoto(Guid eventId, Guid albumId, Guid photoId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Deleting photo {PhotoId} from album {AlbumId} for event {EventId} by user {UserId}",
            photoId, albumId, eventId, userId);

        var command = new DeleteAlbumPhotoCommand(
            AlbumId: albumId,
            PhotoId: photoId,
            RequesterId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Set a photo as the album cover.
    /// Only the event organizer can set the cover photo.
    /// </summary>
    /// <param name="eventId">The event ID (for route consistency).</param>
    /// <param name="albumId">The album ID whose cover to set.</param>
    /// <param name="photoId">The ID of the photo to use as cover.</param>
    [HttpPut("{albumId:guid}/cover/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetAlbumCoverPhoto(Guid eventId, Guid albumId, Guid photoId)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Setting cover photo {PhotoId} for album {AlbumId} on event {EventId} by user {UserId}",
            photoId, albumId, eventId, userId);

        var command = new SetAlbumCoverPhotoCommand(
            AlbumId: albumId,
            PhotoId: photoId,
            UserId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Bulk delete photos from a photo album.
    /// Only the event organizer can perform bulk delete.
    /// Returns the count of successfully deleted photos.
    /// </summary>
    [HttpPost("{albumId:guid}/photos/bulk-delete")]
    [Authorize]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BulkDeleteAlbumPhotos(
        Guid eventId, Guid albumId, [FromBody] BulkDeletePhotosRequest request)
    {
        if (request.PhotoIds == null || request.PhotoIds.Count == 0)
            return BadRequest(new ProblemDetails { Detail = "At least one photo ID is required" });

        if (request.PhotoIds.Count > 50)
            return BadRequest(new ProblemDetails { Detail = "Cannot delete more than 50 photos at once" });

        var userId = User.GetUserId();

        Logger.LogInformation(
            "Bulk deleting {PhotoCount} photos from album {AlbumId} for event {EventId} by user {UserId}",
            request.PhotoIds.Count, albumId, eventId, userId);

        var command = new BulkDeleteAlbumPhotosCommand(
            AlbumId: albumId,
            PhotoIds: request.PhotoIds,
            RequesterId: userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== DOWNLOAD ====================

    /// <summary>
    /// Download all approved photos in an album as a ZIP file.
    /// Streams directly to the response body to handle large albums (up to 200 photos).
    /// Uses original quality images.
    /// </summary>
    [HttpGet("{albumId:guid}/download")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DownloadAlbumZip(Guid eventId, Guid albumId)
    {
        Logger.LogInformation(
            "Downloading album ZIP for album {AlbumId}, event {EventId}",
            albumId, eventId);

        var query = new DownloadAlbumZipQuery(AlbumId: albumId);
        var result = await Mediator.Send(query);

        if (!result.IsSuccess)
            return BadRequest(new ProblemDetails { Detail = result.Error });

        var zipData = result.Value;
        return File(zipData.ZipStream, "application/zip", zipData.FileName);
    }
}

// ==================== REQUEST DTOs ====================

/// <summary>
/// Request body for creating a photo album.
/// </summary>
public class CreatePhotoAlbumRequest
{
    /// <summary>
    /// Display name for the photo album (required).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the photo album.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request body for updating album details.
/// </summary>
public class UpdateAlbumDetailsRequest
{
    /// <summary>
    /// Display name for the photo album (required).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for the photo album.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request body for bulk deleting photos from an album.
/// </summary>
public class BulkDeletePhotosRequest
{
    /// <summary>
    /// List of photo IDs to delete (max 50).
    /// </summary>
    public List<Guid> PhotoIds { get; set; } = new();
}
