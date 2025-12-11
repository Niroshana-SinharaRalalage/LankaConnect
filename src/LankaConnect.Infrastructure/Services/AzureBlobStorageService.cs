using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.9: Azure Blob Storage Service Implementation
/// Handles file uploads/downloads/deletions using Azure Blob Storage
///
/// Configuration Requirements:
/// - AzureStorage:ConnectionString: Azure Storage connection string
/// - AzureStorage:DefaultContainer: Default container name (default: "event-media")
/// </summary>
public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _defaultContainerName;

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;

        // Get connection string from configuration
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure Storage connection string not configured");

        _blobServiceClient = new BlobServiceClient(connectionString);

        // Get default container name (defaults to "event-media")
        _defaultContainerName = configuration["AzureStorage:DefaultContainer"] ?? "event-media";

        _logger.LogInformation("Azure Blob Storage Service initialized with default container: {ContainerName}",
            _defaultContainerName);
    }

    /// <inheritdoc />
    public async Task<(string BlobName, string BlobUrl)> UploadFileAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var container = containerName ?? _defaultContainerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);

            // Ensure container exists (creates if doesn't exist)
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.Blob, // Allows public read access to blobs
                cancellationToken: cancellationToken);

            // Generate unique blob name to avoid conflicts
            var blobName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload file with content type
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                },
                cancellationToken);

            var blobUrl = blobClient.Uri.ToString();

            _logger.LogInformation(
                "File uploaded successfully. BlobName: {BlobName}, URL: {BlobUrl}, Container: {Container}",
                blobName, blobUrl, container);

            return (blobName, blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error uploading file {FileName} to container {Container}",
                fileName, containerName ?? _defaultContainerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var container = containerName ?? _defaultContainerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogInformation(
                    "Blob deleted successfully. BlobName: {BlobName}, Container: {Container}",
                    blobName, container);
            }
            else
            {
                _logger.LogWarning(
                    "Blob not found for deletion. BlobName: {BlobName}, Container: {Container}",
                    blobName, container);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting blob {BlobName} from container {Container}",
                blobName, containerName ?? _defaultContainerName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> BlobExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var container = containerName ?? _defaultContainerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error checking blob existence {BlobName} in container {Container}",
                blobName, containerName ?? _defaultContainerName);
            throw;
        }
    }

    /// <inheritdoc />
    public string GetBlobUrl(string blobName, string? containerName = null)
    {
        var container = containerName ?? _defaultContainerName;
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var blobClient = containerClient.GetBlobClient(blobName);
        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Sanitizes file name to remove invalid characters
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        // Remove invalid characters and spaces
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized.Replace(" ", "_");
    }
}
