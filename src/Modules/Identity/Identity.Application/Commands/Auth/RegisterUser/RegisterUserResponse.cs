using LankaConnect.Domain.Shared.ValueObjects;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RegisterUser;

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    bool EmailVerificationRequired);