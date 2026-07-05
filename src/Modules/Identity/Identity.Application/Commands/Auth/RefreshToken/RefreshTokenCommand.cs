using MediatR;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result<RefreshTokenResponse>>;