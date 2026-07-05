using LankaConnect.Domain.Common;
namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Slice 5: Checks whether a structural layout change (delete zone/table, change shape/geometry,
/// hard-delete layout) is safe given the current hold/reservation state.
///
/// Returns <see cref="ErrorKind.StructuralEditRejected"/> on any conflict so the API layer can
/// map it to HTTP 422 with title <c>layout.structural_edit_rejected</c>.
///
/// Callers pass the set of seat IDs affected by the intended change — that's zone-wide for a
/// zone delete, table-wide for a table delete, or the layout-wide union for a full-layout delete.
/// </summary>
public interface IStructuralEditGuard
{
    /// <summary>
    /// Returns <see cref="Result.Success"/> if none of the given seats are currently held or
    /// reserved. Otherwise returns a failure with <see cref="ErrorKind.StructuralEditRejected"/>
    /// and a descriptive message summarising the conflict counts.
    ///
    /// An empty <paramref name="seatIds"/> short-circuits to success without touching the DB
    /// (useful for changes that don't affect any existing seats yet).
    /// </summary>
    Task<Result> CheckSeatsAsync(IEnumerable<Guid> seatIds, CancellationToken cancellationToken);
}
