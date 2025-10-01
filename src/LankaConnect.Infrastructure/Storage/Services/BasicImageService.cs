using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.Storage.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Infrastructure.Storage.Services;

/// <summary>
/// Basic Azure Blob Storage implementation of IImageService without advanced image processing
/// </summary>
public sealed class BasicImageService : IImageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AzureStorageOptions _options;
    private readonly ILogger<BasicImageService> _logger;

    public BasicImageService(
        BlobServiceClient blobServiceClient,
        IOptions<AzureStorageOptions> options,
        ILogger<BasicImageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _logger = logger;
    }

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

            // Generate unique blob name
            var blobName = GenerateBlobName(fileName, businessId);
            var contentType = GetContentType(fileName);

            // Get blob container
            var containerClient = await GetContainerClientAsync(cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set blob upload options
            var blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    CacheControl = "public, max-age=31536000", // Cache for 1 year
                },
                Metadata = new Dictionary<string, string>
                {
                    ["BusinessId"] = businessId.ToString(),
                    ["OriginalFileName"] = fileName,
                    ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
                    ["FileSize"] = file.Length.ToString()
                }
            };

            // Upload to Azure Blob Storage
            using var stream = new MemoryStream(file);
            await blobClient.UploadAsync(
                stream, 
                blobUploadOptions, 
                cancellationToken);

            var result = new ImageUploadResult
            {
                Url = blobClient.Uri.ToString(),
                BlobName = blobName,
                SizeBytes = file.Length,
                ContentType = contentType,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Image uploaded successfully. BusinessId: {BusinessId}, BlobName: {BlobName}, Size: {Size} bytes",
                businessId, blobName, file.Length);

            return Result<ImageUploadResult>.Success(result);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, 
                "Azure Storage request failed. BusinessId: {BusinessId}, Error: {Error}", 
                businessId, ex.Message);
            return Result<ImageUploadResult>.Failure($"Storage service error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Unexpected error during image upload. BusinessId: {BusinessId}", businessId);
            return Result<ImageUploadResult>.Failure("An unexpected error occurred during image upload");
        }
    }

    public async Task<Result> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract blob name from URL
            var blobName = ExtractBlobNameFromUrl(imageUrl);
            if (string.IsNullOrEmpty(blobName))
                return Result.Failure("Invalid image URL format");

            // Get blob container and client
            var containerClient = await GetContainerClientAsync(cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if blob exists
            var existsResponse = await blobClient.ExistsAsync(cancellationToken);
            if (!existsResponse.Value)
            {
                _logger.LogWarning("Attempted to delete non-existent blob: {BlobName}", blobName);
                return Result.Success(); // Consider non-existent as successfully deleted
            }

            // Delete the blob
            await blobClient.DeleteAsync(
                DeleteSnapshotsOption.IncludeSnapshots, 
                conditions: null, 
                cancellationToken);

            _logger.LogInformation("Image deleted successfully. BlobName: {BlobName}", blobName);
            return Result.Success();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to delete image. URL: {ImageUrl}, Error: {Error}", 
                imageUrl, ex.Message);
            return Result.Failure($"Failed to delete image: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during image deletion. URL: {ImageUrl}", imageUrl);
            return Result.Failure("An unexpected error occurred during image deletion");
        }
    }

    public async Task<Result<string>> GetSecureUrlAsync(
        string imageUrl, 
        int expiresInHours = 24, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract blob name from URL
            var blobName = ExtractBlobNameFromUrl(imageUrl);
            if (string.IsNullOrEmpty(blobName))
                return Result<string>.Failure("Invalid image URL format");

            // For development with Azurite or basic scenarios, return the direct URL
            if (_options.IsDevelopment)
                return Result<string>.Success(imageUrl);

            // Get blob client
            var containerClient = await GetContainerClientAsync(cancellationToken);
            var blobClient = containerClient.GetBlobClient(blobName);

            // Check if we can generate SAS tokens
            if (!blobClient.CanGenerateSasUri)
                return Result<string>.Failure("Unable to generate secure URLs with current storage configuration");

            // Generate SAS token
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.BusinessImagesContainer,
                BlobName = blobName,
                Resource = "b", // Blob resource
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(expiresInHours)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return Result<string>.Success(sasUri.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate secure URL. ImageUrl: {ImageUrl}", imageUrl);
            return Result<string>.Failure("Failed to generate secure URL");
        }
    }

    public Result ValidateImage(byte[] file, string fileName)
    {
        try
        {
            // Check file size
            if (file.Length == 0)
                return Result.Failure("Image file is empty");

            if (file.Length > _options.MaxFileSizeBytes)
                return Result.Failure($"Image file size exceeds maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)}MB");

            // Check file extension
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                return Result.Failure("Invalid image file type. Allowed types: JPG, PNG, GIF, WebP, BMP");

            // Check content type
            var contentType = GetContentType(fileName);
            if (!_options.AllowedContentTypes.Contains(contentType))
                return Result.Failure($"Content type '{contentType}' is not allowed");

            // Basic file header validation for common image types
            if (!IsValidImageFileHeader(file, extension))
                return Result.Failure("Invalid or corrupted image file");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating image file: {FileName}", fileName);
            return Result.Failure("Failed to validate image file");
        }
    }

    public async Task<Result<ImageResizeResult>> ResizeAndUploadAsync(
        byte[] originalImage, 
        string fileName, 
        Guid businessId, 
        CancellationToken cancellationToken = default)
    {
        // For now, just upload the original image without resizing
        // This can be enhanced later when ImageSharp vulnerabilities are resolved
        var uploadResult = await UploadImageAsync(originalImage, fileName, businessId, cancellationToken);
        if (!uploadResult.IsSuccess)
            return Result<ImageResizeResult>.Failure(uploadResult.Errors);

        var result = new ImageResizeResult
        {
            OriginalUrl = uploadResult.Value.Url,
            ThumbnailUrl = uploadResult.Value.Url, // Use original for now
            MediumUrl = uploadResult.Value.Url,    // Use original for now  
            LargeUrl = uploadResult.Value.Url,     // Use original for now
            SizesBytes = new Dictionary<string, long> 
            { 
                ["original"] = uploadResult.Value.SizeBytes,
                ["thumbnail"] = uploadResult.Value.SizeBytes,
                ["medium"] = uploadResult.Value.SizeBytes,
                ["large"] = uploadResult.Value.SizeBytes
            },
            ProcessedAt = DateTime.UtcNow
        };

        return Result<ImageResizeResult>.Success(result);
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.BusinessImagesContainer);
        
        // Ensure container exists
        await containerClient.CreateIfNotExistsAsync(
            PublicAccessType.None, 
            cancellationToken: cancellationToken);
        
        return containerClient;
    }

    private static string GenerateBlobName(string fileName, Guid businessId)
    {
        var extension = Path.GetExtension(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        
        return $"businesses/{businessId}/{timestamp}_{uniqueId}{extension}";
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            _ => "image/jpeg" // Default fallback
        };
    }

    private static string? ExtractBlobNameFromUrl(string imageUrl)
    {
        try
        {
            var uri = new Uri(imageUrl);
            var path = uri.AbsolutePath;
            
            // Remove leading slash and account name for Azure Storage URLs
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2)
            {
                // Skip account name and container name
                return string.Join("/", segments.Skip(2));
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidImageFileHeader(byte[] file, string extension)
    {
        if (file.Length < 4) return false;

        // Check basic file signatures for common image formats
        return extension switch
        {
            ".jpg" or ".jpeg" => file[0] == 0xFF && file[1] == 0xD8 && file[2] == 0xFF,
            ".png" => file[0] == 0x89 && file[1] == 0x50 && file[2] == 0x4E && file[3] == 0x47,
            ".gif" => (file[0] == 0x47 && file[1] == 0x49 && file[2] == 0x46),
            ".bmp" => file[0] == 0x42 && file[1] == 0x4D,
            _ => true // For other formats, assume valid for now
        };
    }
}