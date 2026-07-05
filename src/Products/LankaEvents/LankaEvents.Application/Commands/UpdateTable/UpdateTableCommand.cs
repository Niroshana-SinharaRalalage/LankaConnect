using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateTable;

/// <summary>
/// Slice 5 Chunk 6: PATCH /api/venue-layouts/{id}/tables/{tableId}. All fields
/// optional — only non-null values are applied. Structural changes (shape,
/// capacity, geometry) invoke the <c>IStructuralEditGuard</c> on the table's
/// seats and return HTTP 422 <c>layout.structural_edit_rejected</c> when any
/// seat is held or reserved. <c>ExpectedRowVersion</c> sourced from <c>If-Match</c>.
/// </summary>
public record UpdateTableCommand(
    Guid LayoutId,
    Guid TableId,
    uint ExpectedRowVersion,
    string? Label,
    TableShape? Shape,
    int? Capacity,
    int? SortOrder,
    Guid? ZoneId,
    bool ClearZoneId,
    string? Geometry
) : ICommand;
