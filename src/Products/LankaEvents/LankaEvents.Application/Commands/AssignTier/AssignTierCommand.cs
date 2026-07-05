using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AssignTier;

/// <summary>
/// Slice 5 Chunk 8: POST /api/venue-layouts/{id}/tier-assignments. Links a
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.TicketTier"/> to a <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueZone"/>
/// or <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueTable"/> via the polymorphic
/// <c>tier_assignments</c> junction introduced in Slice 4 Release N.
///
/// Idempotent: re-posting the same tuple is a no-op success (mirrors the domain
/// contract on <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.TicketTier.AssignToZone"/>).
///
/// <para>
/// Concurrency: the <c>If-Match</c> header is compared in-memory against
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout.RowVersion"/>. We do NOT bump
/// the layout's xmin on success — tier assignments do not mutate the layout row,
/// so the EF Core optimistic-concurrency token stays valid for subsequent
/// structural edits.
/// </para>
/// </summary>
public record AssignTierCommand(
    Guid LayoutId,
    uint ExpectedRowVersion,
    Guid TierId,
    AssignableKind Kind,
    Guid AssignableId
) : ICommand;
