using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to clean up expired album photos (7-day retention).
///
/// Purpose:
/// - Storage hygiene: Delete blob storage files for expired photos
/// - Database hygiene: Remove expired AlbumPhoto records and decrement parent album PhotoCount
/// - Runs daily at 2 AM UTC to minimize impact on production traffic
/// - Processes in batches of 100 to avoid long-running transactions
///
/// Retention Policy:
/// - AlbumPhoto entities have ExpiresAt field set to RetentionDays (default 7) from upload
/// - This service deletes photos where ExpiresAt &lt;= NOW()
/// - For each photo, all 3 blob sizes (original, medium, thumbnail) are deleted
/// </summary>
public class AlbumPhotoCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlbumPhotoCleanupService> _logger;
    private const int BatchSize = 100;

    public AlbumPhotoCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<AlbumPhotoCleanupService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlbumPhotoCleanupService started - cleanup runs daily at 2 AM UTC");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate delay until next 2 AM UTC
                var now = DateTime.UtcNow;
                var next2AM = now.Date.AddDays(now.Hour >= 2 ? 1 : 0).AddHours(2);
                var delay = next2AM - now;

                _logger.LogInformation(
                    "Next album photo cleanup scheduled for {NextRun} (in {Hours:F1} hours)",
                    next2AM, delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await CleanupExpiredPhotosAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AlbumPhotoCleanupService stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AlbumPhotoCleanupService - will retry in 30 minutes");
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("AlbumPhotoCleanupService stopped");
    }

    /// <summary>
    /// Processes all expired photos in batches until none remain.
    /// For each batch: delete blobs, remove DB records, decrement album PhotoCount.
    /// </summary>
    private async Task CleanupExpiredPhotosAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting album photo cleanup...");

        var totalDeletedCount = 0;
        var totalBlobSuccessCount = 0;
        var totalBlobFailureCount = 0;

        try
        {
            // Process batches until no more expired photos remain
            while (!cancellationToken.IsCancellationRequested)
            {
                // Create a fresh scope per batch to avoid stale DbContext state
                using var scope = _scopeFactory.CreateScope();
                var photoAlbumRepository = scope.ServiceProvider.GetRequiredService<IPhotoAlbumRepository>();
                var albumImageService = scope.ServiceProvider.GetRequiredService<IAlbumImageService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                var expiredPhotos = await photoAlbumRepository.GetExpiredPhotosAsync(BatchSize, cancellationToken);

                if (expiredPhotos.Count == 0)
                {
                    break;
                }

                _logger.LogInformation(
                    "Processing batch of {Count} expired album photos",
                    expiredPhotos.Count);

                var batchBlobSuccessCount = 0;
                var batchBlobFailureCount = 0;

                // Group by AlbumId to efficiently load and update parent albums
                var photosByAlbum = expiredPhotos.GroupBy(p => p.AlbumId);

                foreach (var albumGroup in photosByAlbum)
                {
                    var albumId = albumGroup.Key;
                    var photosInAlbum = albumGroup.ToList();

                    // Delete blobs for each photo (don't let one failure abort the batch)
                    foreach (var photo in photosInAlbum)
                    {
                        try
                        {
                            var deleteResult = await albumImageService.DeletePhotoAsync(
                                photo.OriginalBlobName,
                                photo.ThumbnailBlobName,
                                photo.MediumBlobName,
                                cancellationToken);

                            if (deleteResult.IsSuccess)
                            {
                                batchBlobSuccessCount++;
                            }
                            else
                            {
                                batchBlobFailureCount++;
                                _logger.LogWarning(
                                    "Failed to delete blobs for photo {PhotoId} in album {AlbumId}: {Error}",
                                    photo.Id, albumId, deleteResult.Error);
                            }
                        }
                        catch (Exception ex)
                        {
                            batchBlobFailureCount++;
                            _logger.LogError(ex,
                                "Exception deleting blobs for photo {PhotoId} in album {AlbumId}. " +
                                "Blobs: {Original}, {Thumbnail}, {Medium}",
                                photo.Id, albumId,
                                photo.OriginalBlobName, photo.ThumbnailBlobName, photo.MediumBlobName);
                        }
                    }

                    // Remove expired photo records from DbContext
                    var efDbContext = (DbContext)dbContext;
                    efDbContext.Set<AlbumPhoto>().RemoveRange(photosInAlbum);

                    // Load the parent album to decrement PhotoCount
                    var album = await efDbContext.Set<PhotoAlbum>()
                        .FirstOrDefaultAsync(a => a.Id == albumId, cancellationToken);

                    if (album != null)
                    {
                        // Only count approved photos toward the decrement
                        var approvedCount = photosInAlbum.Count(p => p.Status == Domain.Events.Enums.AlbumPhotoStatus.Approved);
                        if (approvedCount > 0)
                        {
                            album.DecrementPhotoCount(approvedCount);
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Parent album {AlbumId} not found when decrementing photo count for {Count} expired photos",
                            albumId, photosInAlbum.Count);
                    }
                }

                // Persist all removals and album updates for this batch
                await dbContext.CommitAsync(cancellationToken);

                totalDeletedCount += expiredPhotos.Count;
                totalBlobSuccessCount += batchBlobSuccessCount;
                totalBlobFailureCount += batchBlobFailureCount;

                _logger.LogInformation(
                    "Deleted {Count} expired album photos, blob cleanup {SuccessCount}/{TotalCount} succeeded",
                    expiredPhotos.Count, batchBlobSuccessCount, expiredPhotos.Count);

                // If we got fewer than BatchSize, there are no more to process
                if (expiredPhotos.Count < BatchSize)
                {
                    break;
                }
            }

            if (totalDeletedCount == 0)
            {
                _logger.LogInformation("No expired album photos to clean up");
            }
            else
            {
                _logger.LogInformation(
                    "Album photo cleanup complete. Total deleted: {TotalDeleted}, " +
                    "blob cleanup succeeded: {BlobSuccess}, failed: {BlobFailure}",
                    totalDeletedCount, totalBlobSuccessCount, totalBlobFailureCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed during album photo cleanup. Deleted {TotalDeleted} photos before failure",
                totalDeletedCount);
        }
    }
}
