using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Identity.Application.Commands.Auth.LogoutUser;

public record LogoutUserCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result>;