using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Auth.Commands.RegisterUser;

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    bool EmailVerificationRequired);