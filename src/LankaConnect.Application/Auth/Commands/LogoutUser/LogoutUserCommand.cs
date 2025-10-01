using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Auth.Commands.LogoutUser;

public record LogoutUserCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result>;