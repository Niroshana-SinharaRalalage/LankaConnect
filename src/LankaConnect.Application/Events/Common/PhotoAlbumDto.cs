using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// DTO for photo album metadata.
/// Used for album management and display in event pages.
/// </summary>
public record PhotoAlbumDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid OrganizerId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public AlbumStatus Status { get; init; }
    public AlbumUploadPermission UploadPermission { get; init; }
    public AlbumModerationMode ModerationMode { get; init; }
    public string? Description { get; init; }
    public string? CoverPhotoUrl { get; init; }
    public int RetentionDays { get; init; }
    public int PhotoCount { get; init; }
    public DateTime? PublishedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
