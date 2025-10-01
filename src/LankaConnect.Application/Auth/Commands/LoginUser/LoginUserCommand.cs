using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Auth.Commands.LoginUser;

public record LoginUserCommand(
    string Email,
    string Password,
    string? IpAddress = null) : IRequest<Result<LoginUserResponse>>;