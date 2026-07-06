using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
namespace LankaConnect.Host.AllInOne.Services;

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
    private const int MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB (images)
    private const int MAX_VIDEO_SIZE_BYTES = 500 * 1024 * 1024; // 500 MB (videos)
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

    private static readonly HashSet<string> AllowedVideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov"
    };

    private static readonly HashSet<string> AllowedVideoMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/quicktime"
    };

    // Magic number signatures for file validation
    private static readonly byte[] JpegMagic = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47 };
    private static readonly byte[] GifMagic = { 0x47, 0x49, 0x46, 0x38 };
    private static readonly byte[] WebpMagic = { 0x52, 0x49, 0x46, 0x46 }; // RIFF header

    // Video magic numbers
    private static readonly byte[] WebmMagic = { 0x1A, 0x45, 0xDF, 0xA3 }; // EBML header
    // MP4/MOV: 'ftyp' at offset 4
    private static readonly byte[] FtypMagic = { 0x66, 0x74, 0x79, 0x70 }; // "ftyp" ASCII

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
        string? mediumBlobName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Deleting album media blobs: {Original}, {Thumb}, {Medium}",
            originalBlobName, thumbnailBlobName, mediumBlobName ?? "(none)");

        try
        {
            var tasks = new List<Task>
            {
                _blobStorage.DeleteFileAsync(originalBlobName, CONTAINER_NAME),
                _blobStorage.DeleteFileAsync(thumbnailBlobName, CONTAINER_NAME)
            };

            if (!string.IsNullOrEmpty(mediumBlobName))
                tasks.Add(_blobStorage.DeleteFileAsync(mediumBlobName, CONTAINER_NAME));

            await Task.WhenAll(tasks);

            _logger.LogInformation("Album media blobs deleted successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete album media blobs: {Original}, {Thumb}, {Medium}",
                originalBlobName, thumbnailBlobName, mediumBlobName ?? "(none)");
            return Result.Failure("Failed to delete media files from storage");
        }
    }

    public Result ValidateAlbumVideo(byte[] videoData, string fileName)
    {
        try
        {
            if (videoData == null || videoData.Length == 0)
                return Result.Failure("Video data is empty");

            if (videoData.Length > MAX_VIDEO_SIZE_BYTES)
                return Result.Failure($"Video size exceeds maximum of {MAX_VIDEO_SIZE_BYTES / (1024 * 1024)} MB");

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(extension) || !AllowedVideoExtensions.Contains(extension))
                return Result.Failure($"Video file type '{extension}' is not allowed. Allowed: {string.Join(", ", AllowedVideoExtensions)}");

            if (!IsValidVideoMagicNumber(videoData))
            {
                // Log first 32 bytes hex dump for diagnostics
                var hexDump = BitConverter.ToString(videoData, 0, Math.Min(32, videoData.Length));
                _logger.LogWarning(
                    "Video magic number validation failed for {FileName} ({Size} bytes). First 32 bytes: {HexDump}",
                    fileName, videoData.Length, hexDump);
                return Result.Failure("File content does not match a supported video format");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating album video {FileName}", fileName);
            return Result.Failure("Failed to validate video file");
        }
    }

    public async Task<Result<AlbumVideoUploadResult>> ProcessAndUploadVideoAsync(
        byte[] videoData,
        string videoFileName,
        byte[] thumbnailData,
        string thumbnailFileName,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        var mediaId = Guid.NewGuid();
        var blobPrefix = $"albums/{eventId}/{mediaId}";

        _logger.LogInformation(
            "Processing album video for EventId {EventId}, MediaId {MediaId}, FileName {FileName}, Size {SizeBytes} bytes",
            eventId, mediaId, videoFileName, videoData.Length);

        try
        {
            // Note: Video validation is done by the command handler before calling this method.
            // We only validate the thumbnail here since it's generated client-side.
            var thumbValidation = ValidateAlbumPhoto(thumbnailData, thumbnailFileName);
            if (!thumbValidation.IsSuccess)
                return Result<AlbumVideoUploadResult>.Failure($"Thumbnail validation failed: {thumbValidation.Error}");

            // Process thumbnail: strip EXIF, resize to 150x150, convert to WebP
            byte[] processedThumbnail;
            using (var image = Image.Load(thumbnailData))
            {
                image.Metadata.ExifProfile = null;
                image.Metadata.IccProfile = null;
                image.Metadata.IptcProfile = null;
                image.Metadata.XmpProfile = null;

                image.Mutate(ctx =>
                {
                    ctx.Resize(new ResizeOptions
                    {
                        Size = new Size(THUMBNAIL_SIZE, THUMBNAIL_SIZE),
                        Mode = ResizeMode.Crop,
                        Position = AnchorPositionMode.Center
                    });
                });

                using var ms = new MemoryStream();
                await image.SaveAsWebpAsync(ms, new WebpEncoder { Quality = THUMBNAIL_QUALITY }, cancellationToken);
                processedThumbnail = ms.ToArray();
            }

            _logger.LogInformation(
                "Video thumbnail processed: {ThumbSize}KB for EventId {EventId}",
                processedThumbnail.Length / 1024, eventId);

            // 4. Upload video original to blob storage (no processing)
            var videoExt = Path.GetExtension(videoFileName).ToLowerInvariant();
            var videoContentType = GetVideoContentType(videoExt);

            var (videoBlobName, videoUrl) = await _blobStorage.UploadFileAsync(
                $"{blobPrefix}/video{videoExt}",
                new MemoryStream(videoData),
                videoContentType,
                CONTAINER_NAME);

            // 5. Upload processed thumbnail
            var (thumbBlobName, thumbUrl) = await _blobStorage.UploadFileAsync(
                $"{blobPrefix}/thumb.webp",
                new MemoryStream(processedThumbnail),
                "image/webp",
                CONTAINER_NAME);

            _logger.LogInformation(
                "Album video uploaded successfully. MediaId {MediaId}, EventId {EventId}. Blobs: {Video}, {Thumb}",
                mediaId, eventId, videoBlobName, thumbBlobName);

            return Result<AlbumVideoUploadResult>.Success(new AlbumVideoUploadResult(
                videoUrl, videoBlobName,
                thumbUrl, thumbBlobName,
                videoData.Length));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process and upload album video for EventId {EventId}, MediaId {MediaId}",
                eventId, mediaId);

            // Attempt cleanup of any partially uploaded blobs
            await TryCleanupPartialUpload(blobPrefix, cancellationToken);

            return Result<AlbumVideoUploadResult>.Failure("Failed to process and upload video. Please try again.");
        }
    }

    private bool IsValidVideoMagicNumber(byte[] data)
    {
        if (data.Length < 8) return false;

        // WebM: starts with EBML header 0x1A 0x45 0xDF 0xA3
        if (StartsWith(data, WebmMagic))
            return true;

        // MP4/MOV: Walk ISO BMFF box structure to find 'ftyp' box.
        // The ftyp box is usually first but some encoders insert 'free', 'wide',
        // 'skip', or 'pdin' boxes before it. Scan up to 4096 bytes.
        int offset = 0;
        int limit = Math.Min(data.Length, 4096);
        while (offset + 8 <= limit)
        {
            // Box size is big-endian uint32 at offset
            uint boxSize = (uint)(data[offset] << 24 | data[offset + 1] << 16 |
                                   data[offset + 2] << 8 | data[offset + 3]);

            // Check box type at offset+4 for 'ftyp'
            if (data[offset + 4] == FtypMagic[0] && data[offset + 5] == FtypMagic[1] &&
                data[offset + 6] == FtypMagic[2] && data[offset + 7] == FtypMagic[3])
                return true;

            // Invalid box size — avoid infinite loop
            if (boxSize < 8) break;

            offset += (int)boxSize;
        }

        return false;
    }

    private static string GetVideoContentType(string extension) => extension.ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".mov" => "video/quicktime",
        _ => "application/octet-stream"
    };

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
