using MediatR;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.LogoutUser;

public record LogoutUserCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result>;
