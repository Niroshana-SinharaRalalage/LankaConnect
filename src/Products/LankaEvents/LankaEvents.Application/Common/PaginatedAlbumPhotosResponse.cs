namespace LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;

/// <summary>
/// Paginated response for album photos using cursor-based pagination.
/// Designed for infinite scroll on the frontend.
/// </summary>
public record PaginatedAlbumPhotosResponse
{
    /// <summary>
    /// The list of photos in this page.
    /// </summary>
    public IReadOnlyList<AlbumPhotoDto> Photos { get; init; } = Array.Empty<AlbumPhotoDto>();

    /// <summary>
    /// Whether more photos are available after this page.
    /// </summary>
    public bool HasMore { get; init; }

    /// <summary>
    /// Cursor for the next page (UploadedAt of the last photo in this page).
    /// Null if no more pages.
    /// </summary>
    public DateTime? NextCursor { get; init; }

    /// <summary>
    /// Total count of approved photos in the album.
    /// </summary>
    public int TotalCount { get; init; }
}
