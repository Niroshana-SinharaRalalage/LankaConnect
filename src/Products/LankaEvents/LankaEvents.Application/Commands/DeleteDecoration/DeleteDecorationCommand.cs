using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.DeleteDecoration;

/// <summary>
/// Slice 5 Chunk 7: DELETE /api/venue-layouts/{id}/decorations/{decorationId}.
/// Removes a decoration from a layout. Decorations have no seats, so no
/// structural guard runs. <c>ExpectedRowVersion</c> comes from <c>If-Match</c>.
/// </summary>
public record DeleteDecorationCommand(
    Guid LayoutId,
    Guid DecorationId,
    uint ExpectedRowVersion
) : ICommand;
