namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 6A.9: Azure Blob Storage Service Interface
/// Abstracts Azure Blob Storage operations for image/video uploads
/// </summary>
public interface IAzureBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage
    /// </summary>
    /// <param name="fileName">Name of the file to be stored</param>
    /// <param name="fileStream">Stream containing file data</param>
    /// <param name="contentType">MIME type of the file (e.g., "image/jpeg", "video/mp4")</param>
    /// <param name="containerName">Container name (defaults to "event-media" if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing (blobName, blobUrl)</returns>
    Task<(string BlobName, string BlobUrl)> UploadFileAsync(
        string fileName,
        Stream fileStream,
        string contentType,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from Azure Blob Storage
    /// </summary>
    /// <param name="blobName">Name of the blob to delete</param>
    /// <param name="containerName">Container name (defaults to "event-media" if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully, false if blob not found</returns>
    Task<bool> DeleteFileAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists in Azure Blob Storage
    /// </summary>
    /// <param name="blobName">Name of the blob to check</param>
    /// <param name="containerName">Container name (defaults to "event-media" if not specified)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if blob exists, false otherwise</returns>
    Task<bool> BlobExistsAsync(
        string blobName,
        string? containerName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the URL of a blob
    /// </summary>
    /// <param name="blobName">Name of the blob</param>
    /// <param name="containerName">Container name (defaults to "event-media" if not specified)</param>
    /// <returns>Full URL to the blob</returns>
    string GetBlobUrl(string blobName, string? containerName = null);
}
