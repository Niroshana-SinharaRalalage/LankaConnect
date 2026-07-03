using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

namespace LankaConnect.Modules.Media.Contracts.IntegrationEvents;

/// <summary>
/// Wave 6.5.b canary: fires when an organizer publishes a photo album, making
/// it visible to attendees. Subscribers own downstream side-effects
/// (notification emails, notifications-module feed entries, etc). Producer:
/// <c>PublishPhotoAlbumCommandHandler</c>.
/// </summary>
/// <param name="AlbumId">Photo album identifier (Media aggregate root).</param>
/// <param name="OwningEventId">Owning event identifier (LankaEvents aggregate root).
/// Renamed from EventId to avoid clashing with <see cref="IntegrationEventBase.EventId"/>
/// (the per-event unique identifier used for idempotency + tracing).</param>
/// <param name="EventTitle">Owning event title. Denormalised into the payload
/// so subscribers don't need to reach across module boundaries to fetch it.</param>
/// <param name="AlbumName">Human-readable album name for downstream rendering.</param>
/// <param name="PublishedByUserId">User who published the album (organizer).</param>
/// <remarks>
/// <para>
/// <b>Wire-format immutability</b>: this record is the cross-module ABI. Any
/// field added post-launch requires a <c>V2</c> record per ADR-005 §Versioning.
/// Fields are CLR primitives + <see cref="Guid"/> only — no
/// <c>Media.Domain</c> types, no enums declared outside <c>Media.Contracts</c>.
/// This is the rule that keeps Phase B microservice extraction free.
/// </para>
/// <para>
/// Parallel to the existing <c>PhotoAlbumPublishedDomainEvent</c> raised on
/// the <c>PhotoAlbum</c> aggregate. The domain event fires intra-module; the
/// integration event is the cross-module surface written to the outbox atomically
/// with the aggregate mutation and dispatched via
/// <see cref="OutboxProcessor{TDbContext}"/> to <c>INotificationHandler</c>s
/// subscribing at the outbox dispatcher boundary. D10 boundary preserved.
/// </para>
/// </remarks>
public sealed record PhotoAlbumPublishedIntegrationEventV1(
    Guid AlbumId,
    Guid OwningEventId,
    string EventTitle,
    string AlbumName,
    Guid PublishedByUserId) : IntegrationEventBase, IIntegrationEventV1;
