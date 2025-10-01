using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Businesses.Commands.SetPrimaryBusinessImage;

/// <summary>
/// Command to set a business image as primary
/// </summary>
public sealed record SetPrimaryBusinessImageCommand : IRequest<Result>
{
    public Guid BusinessId { get; init; }
    public string ImageId { get; init; } = string.Empty;

    public SetPrimaryBusinessImageCommand(Guid businessId, string imageId)
    {
        BusinessId = businessId;
        ImageId = imageId;
    }
}