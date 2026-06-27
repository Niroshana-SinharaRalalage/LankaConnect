using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;

namespace LankaConnect.Products.LankaEvents.Domain.Repositories;

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
    /// Slice S1.5: hard-deletes ALL venue_layouts rows whose <c>event_id</c> equals
    /// the given event id, plus the <c>tier_assignments</c> rows referencing those
    /// rows' zones/tables (polymorphic FK has no DB cascade — manual cleanup).
    /// FK cascades handle the rest: zones / tables / seats / decorations all
    /// cascade-delete via <c>OnDelete.Cascade</c> (verified by S1.5 pre-flight).
    ///
    /// <para>
    /// Used by <c>ApplyPresetToEventCommand</c> + <c>ApplyTemplateToEventCommand</c>
    /// to atomically clean up the previously-attached layout AND any orphan rows
    /// before inserting the new one — closes the
    /// <c>ix_venue_layouts_event_id_name</c> unique-constraint collision class.
    /// </para>
    ///
    /// <para>
    /// Returns the count of <c>venue_layouts</c> rows deleted (0 when none matched).
    /// Idempotent. Does NOT verify the structural-edit guard — callers should run
    /// the guard separately if active holds / reservations might exist (the
    /// architect's S2 work — for now, S1.5 trusts that organisers re-apply
    /// presets only on events without live registrations because of
    /// <c>EnableAssignedSeating</c>'s pre-existing
    /// "no registrations exist" rule).
    /// </para>
    /// </summary>
    Task<int> HardDeleteByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

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
