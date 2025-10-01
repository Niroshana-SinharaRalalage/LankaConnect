using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Businesses.Commands.ReorderBusinessImages;

/// <summary>
/// Command to reorder business images
/// </summary>
public sealed record ReorderBusinessImagesCommand : IRequest<Result>
{
    public Guid BusinessId { get; init; }
    public List<string> ImageIds { get; init; } = new();

    public ReorderBusinessImagesCommand(Guid businessId, List<string> imageIds)
    {
        BusinessId = businessId;
        ImageIds = imageIds ?? new List<string>();
    }
}