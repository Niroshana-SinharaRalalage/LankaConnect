using LankaConnect.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateLayout;

/// <summary>
/// Slice 5 Chunk 4: Updates a venue layout's name and/or canvas configuration with
/// optimistic concurrency via <paramref name="ExpectedRowVersion"/> (sourced from the
/// <c>If-Match</c> header). Both fields are optional — only non-null values are applied.
/// Authorization is delegated to <c>ILayoutAuthorizationService</c> (two-branch rule).
/// </summary>
public record UpdateLayoutCommand(
    Guid LayoutId,
    uint ExpectedRowVersion,
    string? Name,
    UpdateLayoutCanvasRequest? Canvas
) : ICommand;

/// <summary>
/// Canvas configuration payload. All fields required when supplied; the API layer
/// surfaces domain validation failures from <c>CanvasConfig.Create</c> verbatim.
/// </summary>
public record UpdateLayoutCanvasRequest(
    int Width,
    int Height,
    double Scale,
    string BackgroundColor
);
