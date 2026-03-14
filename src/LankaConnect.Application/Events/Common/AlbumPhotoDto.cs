using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// DTO for a single photo within a photo album.
/// Contains all 3 image URLs (original, medium, thumbnail) and metadata.
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
    public long FileSizeBytes { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public int DisplayOrder { get; init; }
}
