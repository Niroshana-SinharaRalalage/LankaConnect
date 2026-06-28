using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteZone;

/// <summary>
/// Slice 5 Chunk 5: DELETE /api/venue-layouts/{id}/zones/{zoneId}.
/// Always runs the <c>IStructuralEditGuard</c> on the zone's seats — rejected with
/// HTTP 422 <c>layout.structural_edit_rejected</c> when any seat is held or reserved.
/// <c>ExpectedRowVersion</c> sourced from the <c>If-Match</c> header.
/// </summary>
public record DeleteZoneCommand(
    Guid LayoutId,
    Guid ZoneId,
    uint ExpectedRowVersion
) : ICommand;
