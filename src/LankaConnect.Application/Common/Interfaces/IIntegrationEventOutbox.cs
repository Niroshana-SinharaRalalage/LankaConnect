using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Wave 6.5.a: façade over the module-scoped outbox writer. Handlers enqueue
/// integration events through this interface so their compile-time surface
/// depends only on <see cref="LankaConnect.Application.Common.Interfaces"/>,
/// never on <c>BuildingBlocks.Infrastructure.Outbox</c> directly.
/// </summary>
/// <typeparam name="TDbContext">The module DbContext whose <c>Outbox</c>
/// <see cref="DbSet{T}"/> receives the enqueued row (e.g. <c>MediaDbContext</c>,
/// <c>NotificationsDbContext</c>, <c>FormsDbContext</c>, <c>LankaEventsDbContext</c>).</typeparam>
/// <remarks>
/// <para>
/// <b>Transactional contract</b>: <see cref="EnqueueAsync"/> ADDs the outbox
/// row to the module DbContext's change tracker but does NOT call
/// <c>SaveChangesAsync</c>. The caller's UoW commits the row alongside the
/// originating state change in a single transaction — outbox row + state
/// change land atomically. This is the ADR-005 "outbox everything" contract.
/// </para>
/// <para>
/// <b>Wave 6.5.a-h handler pattern</b>:
/// <code>
/// // 1. Aggregate mutation raises domain event (intra-module)
/// var result = album.Publish();
/// if (result.IsFailure) return result;
///
/// // 2. Enqueue integration event (cross-module wire format)
/// await _outbox.EnqueueAsync(new PhotoAlbumPublishedIntegrationEventV1(
///     album.Id, album.EventId, album.EventTitle, album.Name, userId), ct);
///
/// // 3. Atomic multi-context commit
/// await _unitOfWork.CommitAsync(new DbContext[] { _mediaContext }, ct);
/// </code>
/// </para>
/// <para>
/// <b>Phase B swap-out</b>: when modules deploy independently (post-Wave 8),
/// swap the concrete registration to a Service Bus / Kafka publisher — the
/// interface stays intact, no handler code changes.
/// </para>
/// </remarks>
public interface IIntegrationEventOutbox<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Stages an integration event in the module DbContext's outbox table.
    /// The row is NOT committed until the caller invokes their UoW's
    /// <c>CommitAsync</c>.
    /// </summary>
    /// <param name="integrationEvent">Sealed <c>*IntegrationEventV1</c> record
    /// carrying the wire-format payload. See ADR-005 §5 for the versioning
    /// convention.</param>
    /// <param name="cancellationToken">Unused at enqueue time (this method
    /// only stages an entity in the change tracker); reserved for future
    /// dispatch-side telemetry batching.</param>
    Task EnqueueAsync(IntegrationEventBase integrationEvent, CancellationToken cancellationToken = default);
}
