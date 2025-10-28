using MediatR;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Auth.Commands.LoginWithEntra;

/// <summary>
/// Command to authenticate a user using Microsoft Entra External ID
/// </summary>
public record LoginWithEntraCommand(
    string AccessToken,
    string? IpAddress = null) : IRequest<Result<LoginWithEntraResponse>>;
