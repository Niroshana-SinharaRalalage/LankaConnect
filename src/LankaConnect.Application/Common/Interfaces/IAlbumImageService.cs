using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service for processing and managing photo album media (images and videos).
/// Handles EXIF stripping, thumbnail generation, and blob storage for album media.
/// </summary>
public interface IAlbumImageService
{
    /// <summary>
    /// Validate, strip EXIF, generate thumbnails, and upload image to Azure Blob Storage.
    /// Produces 3 sizes: original (EXIF stripped), medium (800px), thumbnail (150x150).
    /// </summary>
    Task<Result<AlbumPhotoUploadResult>> ProcessAndUploadAsync(
        byte[] imageData,
        string fileName,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate video file before processing.
    /// Checks file size, format, extension, and magic numbers.
    /// </summary>
    Result ValidateAlbumVideo(byte[] videoData, string fileName);

    /// <summary>
    /// Upload a video and its thumbnail to Azure Blob Storage.
    /// Videos are stored as-is (no transcoding). Thumbnail is processed (EXIF strip, resize to 150x150 WebP).
    /// </summary>
    Task<Result<AlbumVideoUploadResult>> ProcessAndUploadVideoAsync(
        byte[] videoData,
        string videoFileName,
        byte[] thumbnailData,
        string thumbnailFileName,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete blob files for a media item (original, thumbnail, and optionally medium).
    /// Medium blob is null for videos.
    /// </summary>
    Task<Result> DeletePhotoAsync(
        string originalBlobName,
        string thumbnailBlobName,
        string? mediumBlobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate image file before processing.
    /// Checks file size, format, extension, and magic numbers.
    /// </summary>
    Result ValidateAlbumPhoto(byte[] imageData, string fileName);
}

/// <summary>
/// Result of album photo upload with all 3 image sizes.
/// </summary>
public record AlbumPhotoUploadResult(
    string OriginalUrl,
    string OriginalBlobName,
    string ThumbnailUrl,
    string ThumbnailBlobName,
    string MediumUrl,
    string MediumBlobName,
    long FileSizeBytes);

/// <summary>
/// Result of album video upload with original video + thumbnail.
/// </summary>
public record AlbumVideoUploadResult(
    string OriginalUrl,
    string OriginalBlobName,
    string ThumbnailUrl,
    string ThumbnailBlobName,
    long FileSizeBytes);
