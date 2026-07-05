using LankaConnect.Products.LankaEvents.Application.Badges.DTOs;
using LankaConnect.BuildingBlocks.Domain;
using MediatR;
namespace LankaConnect.Products.LankaEvents.Application.Badges.Commands.UpdateBadgeImage;

/// <summary>
/// Command to update a badge's image
/// Phase 6A.25: Badge Management System
/// </summary>
public record UpdateBadgeImageCommand : IRequest<Result<BadgeDto>>
{
    public Guid BadgeId { get; init; }
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}
