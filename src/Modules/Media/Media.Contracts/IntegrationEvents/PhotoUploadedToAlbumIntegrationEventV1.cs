using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

namespace LankaConnect.Modules.Media.Contracts.IntegrationEvents;

/// <summary>
/// Wave 6.5.b canary: fires when a photo (or video) is successfully uploaded to
/// an album — after the blob upload has completed and the aggregate mutation
/// has been persisted atomically via the multi-context outbox. Subscribers
/// own downstream side-effects (analytics counters, cache invalidation, thumbnail
/// re-generation triggers, etc). Producers: <c>UploadAlbumPhotoCommandHandler</c>,
/// <c>UploadAlbumVideoCommandHandler</c>.
/// </summary>
/// <param name="AlbumId">Photo album identifier (Media aggregate root).</param>
/// <param name="PhotoId">The newly uploaded photo or video's identifier.</param>
/// <param name="OwningEventId">Owning event identifier (LankaEvents aggregate root).
/// Renamed from EventId to avoid clashing with <see cref="IntegrationEventBase.EventId"/>.</param>
/// <param name="UploaderUserId">User who uploaded the media (organizer).</param>
/// <param name="IsVideo">True when the upload is a video, false for photos.</param>
/// <remarks>
/// <para>
/// Wire-format immutability rules identical to
/// <see cref="PhotoAlbumPublishedIntegrationEventV1"/>. Any additive change
/// requires a V2 record.
/// </para>
/// </remarks>
public sealed record PhotoUploadedToAlbumIntegrationEventV1(
    Guid AlbumId,
    Guid PhotoId,
    Guid OwningEventId,
    Guid UploaderUserId,
    bool IsVideo) : IntegrationEventBase, IIntegrationEventV1;
