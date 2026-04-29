using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events.Repositories;

/// <summary>
/// Repository interface for VenueLayout aggregate operations.
/// VenueLayout is loaded with its zones and seats (full aggregate).
/// </summary>
public interface IVenueLayoutRepository : IRepository<VenueLayout>
{
    /// <summary>
    /// Gets a venue layout by ID with all zones and seats eagerly loaded.
    /// </summary>
    Task<VenueLayout?> GetWithZonesAndSeatsAsync(Guid layoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Slice 9.3: returns the venue layout currently assigned to the event, identified by
    /// <c>events.venue_layout_id</c>. Returns <c>null</c> when the event has no layout
    /// assigned. Loads the full aggregate (zones + tables + seats + decorations) for
    /// editor / picker / availability call sites.
    ///
    /// <para>
    /// This explicitly does NOT match by <c>venue_layouts.event_id</c>. That column
    /// records provenance (which event a layout was originally created for) and may
    /// reference unassigned orphan rows from prior partial-failure flows. Filtering by
    /// it returned phantom layouts to the picker — the bug fixed in Slice 9.3.
    /// </para>
    /// </summary>
    Task<VenueLayout?> GetAssignedLayoutForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all reusable template layouts created by a user.
    /// </summary>
    Task<IReadOnlyList<VenueLayout>> GetTemplatesByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a layout name is already in use for the given event.
    /// </summary>
    Task<bool> NameExistsForEventAsync(string name, Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Slice 5 Chunk 4: Overrides the tracked entity's <c>RowVersion</c> OriginalValue so
    /// EF Core includes the caller-supplied <paramref name="expectedRowVersion"/> in the
    /// UPDATE WHERE clause. On mismatch, <c>SaveChangesAsync</c> throws
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>, which the
    /// handler maps to <see cref="ErrorKind.Conflict"/> → HTTP 409. The layout MUST already
    /// be tracked by the context (i.e. loaded via <see cref="GetByIdAsync"/> without
    /// AsNoTracking); otherwise the call is a no-op.
    /// </summary>
    void SetOriginalRowVersion(VenueLayout layout, uint expectedRowVersion);
}
