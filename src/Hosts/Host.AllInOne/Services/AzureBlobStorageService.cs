using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using LankaConnect.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
namespace LankaConnect.Host.AllInOne.Services;

/// <summary>
/// Phase 6A.9: Azure Blob Storage Service Implementation
/// Handles file uploads/downloads/deletions using Azure Blob Storage
///
/// Phase 6A.103: Updated to support private containers (no public blob access).
/// When a storage account has allowBlobPublicAccess=false, containers are created
/// with PublicAccessType.None and SAS URLs are generated for blob access.
///
/// Configuration Requirements:
/// - AzureStorage:ConnectionString: Azure Storage connection string
/// - AzureStorage:DefaultContainer: Default container name (default: "event-media")
/// - AzureStorage:SasTokenExpiryDays: SAS token expiry in days (default: 365)
/// </summary>
public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly string _defaultContainerName;
    private readonly int _sasTokenExpiryDays;

    // Cache whether each container has public access to avoid repeated checks
    private readonly ConcurrentDictionary<string, bool> _containerPublicAccessCache = new();

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

        // Get SAS token expiry (defaults to 365 days)
        _sasTokenExpiryDays = int.TryParse(configuration["AzureStorage:SasTokenExpiryDays"], out var days) ? days : 365;

        _logger.LogInformation(
            "Azure Blob Storage Service initialized. Container: {ContainerName}, SasExpiryDays: {SasExpiryDays}",
            _defaultContainerName, _sasTokenExpiryDays);
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

            // Phase 6A.103: Resilient container creation
            // Try public access first, fall back to private if storage account disallows public access
            var isPrivate = await EnsureContainerExistsAsync(containerClient, container, cancellationToken);

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

            // Phase 6A.103: Generate SAS URL for private containers, direct URL for public
            string blobUrl;
            if (isPrivate)
            {
                blobUrl = GetBlobSasUrl(blobName, container);
                _logger.LogInformation(
                    "File uploaded to private container. BlobName: {BlobName}, Container: {Container}, SasExpiryDays: {SasExpiryDays}",
                    blobName, container, _sasTokenExpiryDays);
            }
            else
            {
                blobUrl = blobClient.Uri.ToString();
                _logger.LogInformation(
                    "File uploaded to public container. BlobName: {BlobName}, URL: {BlobUrl}, Container: {Container}",
                    blobName, blobUrl, container);
            }

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

    /// <inheritdoc />
    public string GetBlobSasUrl(string blobName, string? containerName = null, TimeSpan? expiresIn = null)
    {
        try
        {
            var container = containerName ?? _defaultContainerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobName);

            // If the client can't generate SAS (e.g., using managed identity without delegation key),
            // fall back to the direct URL
            if (!blobClient.CanGenerateSasUri)
            {
                _logger.LogWarning(
                    "Cannot generate SAS URI for blob {BlobName} in container {Container}. " +
                    "Ensure the BlobServiceClient is initialized with a connection string containing the account key.",
                    blobName, container);
                return blobClient.Uri.ToString();
            }

            var expiry = expiresIn ?? TimeSpan.FromDays(_sasTokenExpiryDays);
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = container,
                BlobName = blobName,
                Resource = "b", // Blob-level SAS
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogDebug(
                "Generated SAS URL for blob {BlobName} in container {Container}, expires: {ExpiresOn}",
                blobName, container, sasBuilder.ExpiresOn);

            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating SAS URL for blob {BlobName} in container {Container}. Falling back to direct URL.",
                blobName, containerName ?? _defaultContainerName);

            // Fall back to direct URL rather than failing the entire operation
            return GetBlobUrl(blobName, containerName);
        }
    }

    /// <summary>
    /// Phase 6A.103: Ensures a blob container exists, handling both public and private storage accounts.
    /// Tries to create with public blob access first. If the storage account disallows public access
    /// (allowBlobPublicAccess=false), falls back to private access (PublicAccessType.None).
    /// </summary>
    /// <returns>True if the container is private (no public access), false if public</returns>
    private async Task<bool> EnsureContainerExistsAsync(
        BlobContainerClient containerClient,
        string containerName,
        CancellationToken cancellationToken)
    {
        // Check cache first
        if (_containerPublicAccessCache.TryGetValue(containerName, out var cachedIsPrivate))
        {
            return cachedIsPrivate;
        }

        try
        {
            // First, check if the container already exists and what its access level is
            if (await containerClient.ExistsAsync(cancellationToken))
            {
                var properties = await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);
                var isPrivate = properties.Value.PublicAccess == PublicAccessType.None;

                _containerPublicAccessCache[containerName] = isPrivate;
                _logger.LogInformation(
                    "Container {ContainerName} exists with access level: {AccessLevel}",
                    containerName, isPrivate ? "Private" : "Public");

                return isPrivate;
            }

            // Container doesn't exist - try creating with public access
            try
            {
                await containerClient.CreateAsync(
                    PublicAccessType.Blob,
                    cancellationToken: cancellationToken);

                _containerPublicAccessCache[containerName] = false;
                _logger.LogInformation(
                    "Created container {ContainerName} with public blob access",
                    containerName);

                return false; // Not private
            }
            catch (RequestFailedException ex) when (ex.ErrorCode == "PublicAccessNotPermitted" || ex.Status == 409 || ex.Status == 403)
            {
                _logger.LogWarning(
                    "Storage account does not allow public blob access (ErrorCode: {ErrorCode}). " +
                    "Creating container {ContainerName} with private access. " +
                    "SAS URLs will be used for blob access.",
                    ex.ErrorCode, containerName);

                // Fall back to private access
                await containerClient.CreateIfNotExistsAsync(
                    PublicAccessType.None,
                    cancellationToken: cancellationToken);

                _containerPublicAccessCache[containerName] = true;
                return true; // Private
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error ensuring container {ContainerName} exists. Attempting private access as fallback.",
                containerName);

            // Last resort: try private access
            try
            {
                await containerClient.CreateIfNotExistsAsync(
                    PublicAccessType.None,
                    cancellationToken: cancellationToken);

                _containerPublicAccessCache[containerName] = true;
                return true;
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx,
                    "Failed to create container {ContainerName} even with private access",
                    containerName);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<Stream?> DownloadBlobStreamAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var container = containerName ?? _defaultContainerName;
            var containerClient = _blobServiceClient.GetBlobContainerClient(container);
            var blobClient = containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning(
                    "Blob not found for download. BlobName: {BlobName}, Container: {Container}",
                    blobName, container);
                return null;
            }

            var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Blob download started. BlobName: {BlobName}, Container: {Container}, ContentLength: {ContentLength}",
                blobName, container, response.Value.Details.ContentLength);

            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error downloading blob {BlobName} from container {Container}",
                blobName, containerName ?? _defaultContainerName);
            throw;
        }
    }

    /// <summary>
    /// Sanitizes file name to remove invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid characters and spaces
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized.Replace(" ", "_");
    }
}
