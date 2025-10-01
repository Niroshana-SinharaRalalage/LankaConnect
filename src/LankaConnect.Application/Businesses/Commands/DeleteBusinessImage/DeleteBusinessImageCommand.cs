using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Businesses.Commands.DeleteBusinessImage;

/// <summary>
/// Command to delete a business image
/// </summary>
public sealed record DeleteBusinessImageCommand : IRequest<Result>
{
    public Guid BusinessId { get; init; }
    public string ImageId { get; init; } = string.Empty;

    public DeleteBusinessImageCommand(Guid businessId, string imageId)
    {
        BusinessId = businessId;
        ImageId = imageId;
    }
}