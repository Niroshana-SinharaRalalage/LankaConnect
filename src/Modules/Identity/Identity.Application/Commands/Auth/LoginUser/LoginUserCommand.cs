using MediatR;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.LoginUser;

public record LoginUserCommand : IRequest<Result<LoginUserResponse>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; } = false;
    public string? IpAddress { get; init; } = null;
}
