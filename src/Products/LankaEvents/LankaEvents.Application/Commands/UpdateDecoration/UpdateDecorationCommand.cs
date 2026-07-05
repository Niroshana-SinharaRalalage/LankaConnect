using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateDecoration;

/// <summary>
/// Slice 5 Chunk 7: PATCH /api/venue-layouts/{id}/decorations/{decorationId}.
/// All fields optional — null means "keep current". <c>ClearLabel=true</c>
/// explicitly detaches the label (only valid for non-Text kinds — domain will
/// reject Text + null label). <c>ExpectedRowVersion</c> comes from
/// <c>If-Match</c>. Decorations have no seats, so no structural guard runs.
/// </summary>
public record UpdateDecorationCommand(
    Guid LayoutId,
    Guid DecorationId,
    uint ExpectedRowVersion,
    DecorationKind? Kind,
    string? Label,
    bool ClearLabel,
    int? SortOrder,
    string? Geometry,
    string? Properties
) : ICommand;
