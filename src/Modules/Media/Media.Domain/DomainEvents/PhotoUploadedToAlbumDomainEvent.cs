using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Contracts;

namespace LankaConnect.Modules.Media.Domain.DomainEvents;

/// <summary>
/// Domain event raised when a photo is uploaded to an album.
/// </summary>
public record PhotoUploadedToAlbumDomainEvent(Guid AlbumId, Guid PhotoId, Guid UploaderId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
