using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Image processing service for photo album photos.
/// - Strips ALL EXIF metadata (GPS, camera, timestamps) for privacy
/// - Generates 3 sizes: original (EXIF stripped), medium (800px), thumbnail (150x150)
/// - Converts thumbnails/medium to WebP for smaller file sizes
/// - Uploads to separate 'event-albums' blob container
/// </summary>
public class AlbumImageService : IAlbumImageService
{
    private readonly IAzureBlobStorageService _blobStorage;
    private readonly ILogger<AlbumImageService> _logger;

    private const string CONTAINER_NAME = "event-albums";
    private const int MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB
    private const int THUMBNAIL_SIZE = 150;
    private const int MEDIUM_MAX_WIDTH = 800;
    private const int THUMBNAIL_QUALITY = 80;
    private const int MEDIUM_QUALITY = 85;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp"
    };

    // Magic number signatures for file validation
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47 };
    private static readonly byte[] GifMagic = { 0x47, 0x49, 0x46, 0x38 };
    private static readonly byte[] WebpMagic = { 0x52, 0x49, 0x46, 0x46 }; // RIFF header

    public AlbumImageService(
        IAzureBlobStorageService blobStorage,
        ILogger<AlbumImageService> logger)
    {
        _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result ValidateAlbumPhoto(byte[] imageData, string fileName)
    {
        try
        {
            if (imageData == null || imageData.Length == 0)
                return Result.Failure("Image data is empty");

            if (imageData.Length > MAX_FILE_SIZE_BYTES)
                return Result.Failure($"Image size exceeds maximum of {MAX_FILE_SIZE_BYTES / (1024 * 1024)} MB");

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                return Result.Failure($"File type '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}");

            if (!IsValidImageMagicNumber(imageData))
                return Result.Failure("File content does not match a supported image format");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating album photo {FileName}", fileName);
            return Result.Failure("Failed to validate image file");
        }
    }

    public async Task<Result<AlbumPhotoUploadResult>> ProcessAndUploadAsync(
        byte[] imageData,
        string fileName,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var photoId = Guid.NewGuid();
        var blobPrefix = $"albums/{eventId}/{photoId}";

        _logger.LogInformation(
            "Processing album photo for EventId {EventId}, PhotoId {PhotoId}, FileName {FileName}, Size {SizeBytes} bytes",
            eventId, photoId, fileName, imageData.Length);

        try
        {
            // 1. Validate
            var validationResult = ValidateAlbumPhoto(imageData, fileName);
            if (!validationResult.IsSuccess)
                return Result<AlbumPhotoUploadResult>.Failure(validationResult.Errors);

            byte[] originalStripped;
            byte[] thumbnailData;
            byte[] mediumData;

            // 2. Process image: strip EXIF + generate sizes
            using (var image = Image.Load(imageData))
            {
                _logger.LogDebug("Image loaded: {Width}x{Height}, PixelType: {PixelType}",
                    image.Width, image.Height, image.Metadata.DecodedImageFormat?.Name);

                // Strip ALL EXIF metadata (GPS, camera info, timestamps)
                image.Metadata.ExifProfile = null;
                image.Metadata.IccProfile = null;
                image.Metadata.IptcProfile = null;
                image.Metadata.XmpProfile = null;

                // 2a. Original: EXIF stripped, keep original format & dimensions
                using (var ms = new MemoryStream())
                {
                    var format = image.Metadata.DecodedImageFormat;
                    if (format != null)
                        await image.SaveAsync(ms, format, cancellationToken);
                    else
                        await image.SaveAsPngAsync(ms, cancellationToken);
                    originalStripped = ms.ToArray();
                }

                // 2b. Thumbnail: 150x150 center-crop, WebP
                using (var thumbImage = image.Clone(ctx =>
                {
                    // Resize to cover 150x150, then crop to exact size
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(THUMBNAIL_SIZE, THUMBNAIL_SIZE),
                        Mode = ResizeMode.Crop,
                        Position = AnchorPositionMode.Center
                    });
                }))
                using (var ms = new MemoryStream())
                {
                    await thumbImage.SaveAsWebpAsync(ms, new WebpEncoder { Quality = THUMBNAIL_QUALITY }, cancellationToken);
                    thumbnailData = ms.ToArray();
                }

                // 2c. Medium: max 800px wide (maintain aspect ratio), WebP
                using (var mediumImage = image.Clone(ctx =>
                {
                    if (image.Width > MEDIUM_MAX_WIDTH)
                    {
                        ctx.Resize(new ResizeOptions
                        {
                            Size = new Size(MEDIUM_MAX_WIDTH, 0), // 0 = auto height
                            Mode = ResizeMode.Max
                        });
                    }
                }))
                using (var ms = new MemoryStream())
                {
                    await mediumImage.SaveAsWebpAsync(ms, new WebpEncoder { Quality = MEDIUM_QUALITY }, cancellationToken);
                    mediumData = ms.ToArray();
                }
            }

            _logger.LogInformation(
                "Image processed: Original {OrigSize}KB, Medium {MedSize}KB, Thumbnail {ThumbSize}KB",
                originalStripped.Length / 1024, mediumData.Length / 1024, thumbnailData.Length / 1024);

            // 3. Upload all 3 sizes to Azure Blob Storage
            var originalExt = Path.GetExtension(fileName).ToLowerInvariant();
            var originalContentType = GetContentType(originalExt);

            var (originalBlobName, originalUrl) = await _blobStorage.UploadFileAsync(
                $"{blobPrefix}/original{originalExt}",
                new MemoryStream(originalStripped),
                originalContentType,
                CONTAINER_NAME);

            var (thumbBlobName, thumbUrl) = await _blobStorage.UploadFileAsync(
                $"{blobPrefix}/thumb.webp",
                new MemoryStream(thumbnailData),
                "image/webp",
                CONTAINER_NAME);

            var (mediumBlobName, mediumUrl) = await _blobStorage.UploadFileAsync(
                $"{blobPrefix}/medium.webp",
                new MemoryStream(mediumData),
                "image/webp",
                CONTAINER_NAME);

            _logger.LogInformation(
                "Album photo uploaded successfully. PhotoId {PhotoId}, EventId {EventId}. Blobs: {Original}, {Thumb}, {Medium}",
                photoId, eventId, originalBlobName, thumbBlobName, mediumBlobName);

            return Result<AlbumPhotoUploadResult>.Success(new AlbumPhotoUploadResult(
                originalUrl, originalBlobName,
                thumbUrl, thumbBlobName,
                mediumUrl, mediumBlobName,
                imageData.Length));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process and upload album photo for EventId {EventId}, PhotoId {PhotoId}",
                eventId, photoId);

            // Attempt cleanup of any partially uploaded blobs
            await TryCleanupPartialUpload(blobPrefix, cancellationToken);

            return Result<AlbumPhotoUploadResult>.Failure("Failed to process and upload image. Please try again.");
        }
    }

    public async Task<Result> DeletePhotoAsync(
        string originalBlobName,
        string thumbnailBlobName,
        string mediumBlobName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Deleting album photo blobs: {Original}, {Thumb}, {Medium}",
            originalBlobName, thumbnailBlobName, mediumBlobName);

        try
        {
            var tasks = new[]
            {
                _blobStorage.DeleteFileAsync(originalBlobName, CONTAINER_NAME),
                _blobStorage.DeleteFileAsync(thumbnailBlobName, CONTAINER_NAME),
                _blobStorage.DeleteFileAsync(mediumBlobName, CONTAINER_NAME)
            };

            await Task.WhenAll(tasks);

            _logger.LogInformation("Album photo blobs deleted successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete album photo blobs: {Original}, {Thumb}, {Medium}",
                originalBlobName, thumbnailBlobName, mediumBlobName);
            return Result.Failure("Failed to delete photo files from storage");
        }
    }

    private bool IsValidImageMagicNumber(byte[] data)
    {
        if (data.Length < 4) return false;

        return StartsWith(data, JpegMagic) ||
               StartsWith(data, PngMagic) ||
               StartsWith(data, GifMagic) ||
               StartsWith(data, WebpMagic);
    }

    private static bool StartsWith(byte[] data, byte[] magic)
    {
        if (data.Length < magic.Length) return false;
        for (int i = 0; i < magic.Length; i++)
        {
            if (data[i] != magic[i]) return false;
        }
        return true;
    }

    private static string GetContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };

    private async Task TryCleanupPartialUpload(string blobPrefix, CancellationToken cancellationToken)
    {
        try
        {
            // Best-effort cleanup — don't let cleanup errors mask the original error
            await _blobStorage.DeleteFileAsync($"{blobPrefix}/original*", CONTAINER_NAME);
            await _blobStorage.DeleteFileAsync($"{blobPrefix}/thumb.webp", CONTAINER_NAME);
            await _blobStorage.DeleteFileAsync($"{blobPrefix}/medium.webp", CONTAINER_NAME);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Partial cleanup failed for blob prefix {BlobPrefix}. Blobs may need manual cleanup.", blobPrefix);
        }
    }
}
