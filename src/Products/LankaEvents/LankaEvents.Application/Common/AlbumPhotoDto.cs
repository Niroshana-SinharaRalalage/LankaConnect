using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;

namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// DTO for a single media item (photo or video) within a photo album.
/// Contains image/video URLs, media type discriminator, and metadata.
/// </summary>
public record AlbumPhotoDto
{
    public Guid Id { get; init; }
    public Guid AlbumId { get; init; }
    public Guid UploaderId { get; init; }
    public string UploaderName { get; init; } = string.Empty;
    public string OriginalUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string MediumUrl { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public AlbumPhotoStatus Status { get; init; }
    public AlbumMediaType MediaType { get; init; } = AlbumMediaType.Photo;
    public long FileSizeBytes { get; init; }
    public long? DurationSeconds { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public int DisplayOrder { get; init; }
}
