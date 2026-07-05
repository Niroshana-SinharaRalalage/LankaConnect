using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteTable;

/// <summary>
/// Slice 5 Chunk 6: DELETE /api/venue-layouts/{id}/tables/{tableId}. Always
/// structural — the <c>IStructuralEditGuard</c> rejects the request with HTTP
/// 422 when any seat on the table is held or reserved.
/// <c>ExpectedRowVersion</c> sourced from the <c>If-Match</c> header.
/// </summary>
public record DeleteTableCommand(
    Guid LayoutId,
    Guid TableId,
    uint ExpectedRowVersion
) : ICommand;
