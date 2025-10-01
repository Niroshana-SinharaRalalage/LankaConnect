using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service for managing business images with Azure Blob Storage
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Uploads an image file to Azure Blob Storage
    /// </summary>
    /// <param name="file">Image file data</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="businessId">Business identifier for organizing images</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result containing the image URL and metadata</returns>
    Task<Result<ImageUploadResult>> UploadImageAsync(
        byte[] file, 
        string fileName, 
        Guid businessId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from Azure Blob Storage
    /// </summary>
    /// <param name="imageUrl">Full URL of the image to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> DeleteImageAsync(string imageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a pre-signed URL for secure image access
    /// </summary>
    /// <param name="imageUrl">Image URL</param>
    /// <param name="expiresInHours">URL expiration time in hours (default: 24)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pre-signed URL</returns>
    Task<Result<string>> GetSecureUrlAsync(
        string imageUrl, 
        int expiresInHours = 24, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates image file before upload
    /// </summary>
    /// <param name="file">Image file data</param>
    /// <param name="fileName">File name with extension</param>
    /// <returns>Validation result</returns>
    Result ValidateImage(byte[] file, string fileName);

    /// <summary>
    /// Resizes image to multiple sizes (thumbnail, medium, large)
    /// </summary>
    /// <param name="originalImage">Original image data</param>
    /// <param name="fileName">File name</param>
    /// <param name="businessId">Business identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Multiple sized image URLs</returns>
    Task<Result<ImageResizeResult>> ResizeAndUploadAsync(
        byte[] originalImage, 
        string fileName, 
        Guid businessId, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of image upload operation
/// </summary>
public record ImageUploadResult
{
    public string Url { get; init; } = string.Empty;
    public string BlobName { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

/// <summary>
/// Result of image resize operation with multiple sizes
/// </summary>
public record ImageResizeResult
{
    public string OriginalUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string MediumUrl { get; init; } = string.Empty;
    public string LargeUrl { get; init; } = string.Empty;
    public Dictionary<string, long> SizesBytes { get; init; } = new();
    public DateTime ProcessedAt { get; init; }
}