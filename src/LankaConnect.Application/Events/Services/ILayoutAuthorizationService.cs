using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Slice 5: Centralizes the two-branch authorization rule for VenueLayout CRUD endpoints.
///
/// Branches:
/// 1. Event-attached layout (EventId IS NOT NULL) → caller must be an organizer of the
///    owning event (primary or linked co-organizer).
/// 2. Template layout (EventId IS NULL) → caller must be the layout's
///    <see cref="VenueLayout.CreatedByUserId"/>.
///
/// Admin role bypasses both branches. Missing layouts return <see cref="ErrorKind.NotFound"/>.
/// Unauthenticated callers return <see cref="ErrorKind.Forbidden"/>.
/// Failures use the new ErrorKind-aware Result factories so the API layer maps to 403/404
/// without string-sniffing (see Slice 5 Chunk 1).
/// </summary>
public interface ILayoutAuthorizationService
{
    /// <summary>
    /// Loads the layout by ID and authorizes the current caller against it. Use when the
    /// command handler doesn't already have the aggregate loaded.
    /// </summary>
    Task<Result<VenueLayout>> AuthorizeAsync(Guid layoutId, CancellationToken cancellationToken);

    /// <summary>
    /// Authorizes the current caller against an already-loaded layout. Use when the command
    /// handler has already fetched the aggregate (e.g. via <c>GetWithZonesAndSeatsAsync</c>)
    /// to avoid a duplicate DB round-trip.
    /// </summary>
    Task<Result> AuthorizeLayoutAsync(VenueLayout layout, CancellationToken cancellationToken);
}
