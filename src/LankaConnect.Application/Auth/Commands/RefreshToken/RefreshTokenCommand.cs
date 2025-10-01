using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null) : IRequest<Result<RefreshTokenResponse>>;