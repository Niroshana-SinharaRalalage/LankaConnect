using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result<RefreshTokenResponse>>;