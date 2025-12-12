using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Domain.Badges.Enums;
using LankaConnect.Domain.Common;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.CreateBadge;

/// <summary>
/// Command to create a new badge with uploaded image
/// Phase 6A.25: Badge Management System
/// </summary>
public record CreateBadgeCommand : IRequest<Result<BadgeDto>>
{
    public string Name { get; init; } = string.Empty;
    public BadgePosition Position { get; init; } = BadgePosition.TopRight;
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
}
