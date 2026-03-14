using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service for processing and managing photo album images.
/// Handles EXIF stripping, thumbnail generation, and blob storage for album photos.
/// </summary>
public interface IAlbumImageService
{
    /// <summary>
    /// Validate, strip EXIF, generate thumbnails, and upload to Azure Blob Storage.
    /// Produces 3 sizes: original (EXIF stripped), medium (800px), thumbnail (150x150).
    /// </summary>
    Task<Result<AlbumPhotoUploadResult>> ProcessAndUploadAsync(
        byte[] imageData,
        string fileName,
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete all 3 blob sizes for a photo (original, thumbnail, medium).
    /// </summary>
    Task<Result> DeletePhotoAsync(
        string originalBlobName,
        string thumbnailBlobName,
        string mediumBlobName,
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
