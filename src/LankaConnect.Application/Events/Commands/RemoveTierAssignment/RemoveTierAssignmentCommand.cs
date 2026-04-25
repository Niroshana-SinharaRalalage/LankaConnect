using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.RemoveTierAssignment;

/// <summary>
/// Slice 5 Chunk 8: DELETE /api/venue-layouts/{id}/tier-assignments/{tierId}/{kind}/{assignableId}.
/// Unlinks a <see cref="Domain.Events.Entities.TicketTier"/> from a
/// <see cref="Domain.Events.Entities.VenueZone"/> or <see cref="Domain.Events.Entities.VenueTable"/>.
///
/// Returns <c>NotFound</c> when no matching assignment exists so callers can
/// surface "stale UI state" to the user.
///
/// Concurrency model mirrors <see cref="AssignTier.AssignTierCommand"/>: the
/// <c>If-Match</c> header is compared to the layout's in-memory <c>RowVersion</c>
/// before the mutation.
/// </summary>
public record RemoveTierAssignmentCommand(
    Guid LayoutId,
    uint ExpectedRowVersion,
    Guid TierId,
    AssignableKind Kind,
    Guid AssignableId
) : ICommand;
