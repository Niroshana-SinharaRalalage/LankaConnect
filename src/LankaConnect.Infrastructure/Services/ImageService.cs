using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.9: Image Service Implementation
/// High-level image management service that wraps Azure Blob Storage operations
/// with image-specific validation and processing logic
/// </summary>
public class ImageService : IImageService
{
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<ImageService> _logger;

    // Image validation constraints
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/gif", "image/webp" };

    public ImageService(
        IAzureBlobStorageService blobStorageService,
        ILogger<ImageService> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Result ValidateImage(byte[] file, string fileName)
    {
        try
        {
            // Check file size
            if (file == null || file.Length == 0)
                return Result.Failure("Image file cannot be empty");

            if (file.Length > MaxFileSizeBytes)
                return Result.Failure($"Image file size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB");

            // Check file extension
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
                return Result.Failure($"Invalid image file type. Allowed types: {string.Join(", ", AllowedExtensions)}");

            // Basic content validation (check for image magic numbers)
            if (!IsValidImageFormat(file))
                return Result.Failure("File does not appear to be a valid image");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image {FileName}", fileName);
            return Result.Failure("Error validating image file");
        }
    }

    /// <inheritdoc />
    public async Task<Result<ImageUploadResult>> UploadImageAsync(
        byte[] file,
        string fileName,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate image first
            var validationResult = ValidateImage(file, fileName);
            if (!validationResult.IsSuccess)
                return Result<ImageUploadResult>.Failure(validationResult.Errors);

            // Determine content type
            var contentType = GetContentType(fileName);

            // Upload to Azure Blob Storage
            using var memoryStream = new MemoryStream(file);
            var (blobName, blobUrl) = await _blobStorageService.UploadFileAsync(
                fileName,
                memoryStream,
                contentType,
                cancellationToken: cancellationToken);

            var result = new ImageUploadResult
            {
                Url = blobUrl,
                BlobName = blobName,
                SizeBytes = file.Length,
                ContentType = contentType,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Image uploaded successfully for business {BusinessId}. URL: {Url}, Size: {Size} bytes",
                businessId, blobUrl, file.Length);

            return Result<ImageUploadResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error uploading image {FileName} for business {BusinessId}",
                fileName, businessId);
            return Result<ImageUploadResult>.Failure("Failed to upload image. Please try again.");
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteImageAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return Result.Failure("Image URL cannot be empty");

            // Extract blob name from URL
            var blobName = ExtractBlobNameFromUrl(imageUrl);
            if (string.IsNullOrEmpty(blobName))
                return Result.Failure("Invalid image URL format");

            // Delete from Azure Blob Storage
            var deleted = await _blobStorageService.DeleteFileAsync(
                blobName,
                cancellationToken: cancellationToken);

            if (!deleted)
                _logger.LogWarning("Blob {BlobName} not found for deletion (URL: {ImageUrl})", blobName, imageUrl);

            _logger.LogInformation("Image deleted successfully: {ImageUrl}", imageUrl);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageUrl}", imageUrl);
            return Result.Failure("Failed to delete image. Please try again.");
        }
    }

    /// <inheritdoc />
    public Task<Result<string>> GetSecureUrlAsync(
        string imageUrl,
        int expiresInHours = 24,
        CancellationToken cancellationToken = default)
    {
        // For public blob storage, return the same URL
        // In a production scenario with private containers, you would generate a SAS token here
        _logger.LogInformation("Secure URL requested for {ImageUrl} (expires in {Hours} hours)", imageUrl, expiresInHours);
        return Task.FromResult(Result<string>.Success(imageUrl));
    }

    /// <inheritdoc />
    public async Task<Result<ImageResizeResult>> ResizeAndUploadAsync(
        byte[] originalImage,
        string fileName,
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        // Image resizing not implemented in Phase 6A.9
        // This would require an image processing library like ImageSharp or SkiaSharp
        // For now, just upload the original image
        _logger.LogWarning("Image resizing not yet implemented. Uploading original image only.");

        var uploadResult = await UploadImageAsync(originalImage, fileName, businessId, cancellationToken);
        if (!uploadResult.IsSuccess)
            return Result<ImageResizeResult>.Failure(uploadResult.Errors);

        var result = new ImageResizeResult
        {
            OriginalUrl = uploadResult.Value.Url,
            ThumbnailUrl = uploadResult.Value.Url, // Same as original for now
            MediumUrl = uploadResult.Value.Url,
            LargeUrl = uploadResult.Value.Url,
            SizesBytes = new Dictionary<string, long>
            {
                { "original", uploadResult.Value.SizeBytes }
            },
            ProcessedAt = DateTime.UtcNow
        };

        return Result<ImageResizeResult>.Success(result);
    }

    /// <summary>
    /// Checks if the file has a valid image format by examining magic numbers (file signatures)
    /// </summary>
    private bool IsValidImageFormat(byte[] file)
    {
        if (file.Length < 4)
            return false;

        // Check common image file signatures (magic numbers)
        // JPEG: FF D8 FF
        if (file[0] == 0xFF && file[1] == 0xD8 && file[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47
        if (file[0] == 0x89 && file[1] == 0x50 && file[2] == 0x4E && file[3] == 0x47)
            return true;

        // GIF: 47 49 46 38
        if (file[0] == 0x47 && file[1] == 0x49 && file[2] == 0x46 && file[3] == 0x38)
            return true;

        // WebP: 52 49 46 46 (RIFF) ... 57 45 42 50 (WEBP)
        if (file[0] == 0x52 && file[1] == 0x49 && file[2] == 0x46 && file[3] == 0x46)
        {
            if (file.Length >= 12 && file[8] == 0x57 && file[9] == 0x45 && file[10] == 0x42 && file[11] == 0x50)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines content type based on file extension
    /// </summary>
    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Extracts blob name from full Azure Blob Storage URL
    /// Example: https://account.blob.core.windows.net/container/blobname.jpg → blobname.jpg
    /// </summary>
    private string ExtractBlobNameFromUrl(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl);
            var segments = uri.Segments;
            // Last segment is the blob name
            return segments[segments.Length - 1];
        }
        catch
        {
            return string.Empty;
        }
    }
}
