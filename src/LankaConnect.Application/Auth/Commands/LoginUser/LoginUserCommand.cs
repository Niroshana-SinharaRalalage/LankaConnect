using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Auth.Commands.LoginUser;

public record LoginUserCommand(
    string Email,
    string Password,
    bool RememberMe = false,
    string? IpAddress = null) : IRequest<Result<LoginUserResponse>>;