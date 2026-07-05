using MediatR;
using LankaConnect.Domain.Common;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.LoginWithEntra;

/// <summary>
/// Command to authenticate a user using Microsoft Entra External ID
/// </summary>
public record LoginWithEntraCommand(
    string AccessToken,
    string? IpAddress = null) : IRequest<Result<LoginWithEntraResponse>>;
