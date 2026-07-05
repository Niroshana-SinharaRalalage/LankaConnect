using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AddDecoration;

/// <summary>
/// Slice 5 Chunk 7: POST /api/venue-layouts/{id}/decorations. Adds a non-seating
/// decorative or structural element (stage, dance floor, aisle, door, wall, text,
/// image) to a layout. Decorations have no seats, so the structural-edit guard is
/// not involved. <c>ExpectedRowVersion</c> comes from the <c>If-Match</c> header.
/// </summary>
public record AddDecorationCommand(
    Guid LayoutId,
    uint ExpectedRowVersion,
    DecorationKind Kind,
    string? Label,
    int SortOrder,
    string? Geometry,
    string? Properties
) : ICommand<Guid>;
