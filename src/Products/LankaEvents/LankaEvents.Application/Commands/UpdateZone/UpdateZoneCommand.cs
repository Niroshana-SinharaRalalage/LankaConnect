using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateZone;

/// <summary>
/// Slice 5 Chunk 5: PATCH /api/venue-layouts/{id}/zones/{zoneId}. All fields optional —
/// only non-null values are applied. If <see cref="Shape"/> or <see cref="Geometry"/>
/// changes, the <c>IStructuralEditGuard</c> rejects the request when any seat in the
/// zone is held or reserved (HTTP 422 <c>layout.structural_edit_rejected</c>).
/// <c>ExpectedRowVersion</c> sourced from the <c>If-Match</c> header.
/// </summary>
public record UpdateZoneCommand(
    Guid LayoutId,
    Guid ZoneId,
    uint ExpectedRowVersion,
    string? Name,
    string? Color,
    int? SortOrder,
    ZoneShape? Shape,
    string? Geometry
) : ICommand;
