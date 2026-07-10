using System.IO.Compression;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.PhotoAlbums.DownloadAlbumZip;

/// <summary>
/// Data returned from DownloadAlbumZipQuery containing the ZIP stream and filename.
/// </summary>
public record AlbumZipResult(Stream ZipStream, string FileName);

/// <summary>
/// Query to generate a ZIP file of all approved photos in an album at original quality.
/// Uses streaming approach: writes photos directly into a ZipArchive on a MemoryStream.
/// </summary>
public record DownloadAlbumZipQuery(Guid AlbumId) : ICommand<AlbumZipResult>;

public class DownloadAlbumZipQueryHandler : ICommandHandler<DownloadAlbumZipQuery, AlbumZipResult>
{
    private readonly IPhotoAlbumRepository _photoAlbumRepository;
    private readonly IAzureBlobStorageService _blobStorageService;
    private readonly ILogger<DownloadAlbumZipQueryHandler> _logger;

    public DownloadAlbumZipQueryHandler(
        IPhotoAlbumRepository photoAlbumRepository,
        IAzureBlobStorageService blobStorageService,
        ILogger<DownloadAlbumZipQueryHandler> logger)
    {
        _photoAlbumRepository = photoAlbumRepository;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    public async Task<Result<AlbumZipResult>> Handle(DownloadAlbumZipQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "DownloadAlbumZip"))
        using (LogContext.PushProperty("AlbumId", request.AlbumId))
        {
            _logger.LogInformation("Generating ZIP for album {AlbumId}", request.AlbumId);

            var album = await _photoAlbumRepository.GetByIdAsync(request.AlbumId, trackChanges: false, cancellationToken);
            if (album == null)
                return Result<AlbumZipResult>.Failure($"Album with ID {request.AlbumId} not found");

            if (album.PhotoCount == 0)
                return Result<AlbumZipResult>.Failure("Album has no photos to download");

            var approvedPhotos = album.Photos
                .Where(p => p.Status == AlbumPhotoStatus.Approved)
                .OrderBy(p => p.DisplayOrder)
                .ToList();

            if (approvedPhotos.Count == 0)
                return Result<AlbumZipResult>.Failure("Album has no approved photos to download");

            // Stream ZIP creation
            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var photoIndex = 0;
                foreach (var photo in approvedPhotos)
                {
                    try
                    {
                        photoIndex++;
                        var blobStream = await _blobStorageService.DownloadBlobStreamAsync(
                            photo.OriginalBlobName, cancellationToken: cancellationToken);

                        if (blobStream == null)
                        {
                            _logger.LogWarning(
                                "Blob not found for photo {PhotoId} (blob: {BlobName}), skipping",
                                photo.Id, photo.OriginalBlobName);
                            continue;
                        }

                        // Generate a clean filename
                        var extension = Path.GetExtension(photo.OriginalBlobName);
                        if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                        var entryName = $"photo_{photoIndex:D3}{extension}";

                        var entry = archive.CreateEntry(entryName, CompressionLevel.NoCompression);
                        using var entryStream = entry.Open();
                        await blobStream.CopyToAsync(entryStream, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to add photo {PhotoId} to ZIP. Continuing with remaining photos.",
                            photo.Id);
                    }
                }
            }

            memoryStream.Position = 0;

            // Sanitize album name for filename
            var safeName = string.Join("_", album.Name.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeName}_photos.zip";

            _logger.LogInformation(
                "ZIP generated for album {AlbumId}: {PhotoCount} photos, size: {SizeBytes} bytes",
                request.AlbumId, approvedPhotos.Count, memoryStream.Length);

            return Result<AlbumZipResult>.Success(new AlbumZipResult(memoryStream, fileName));
        }
    }
}
