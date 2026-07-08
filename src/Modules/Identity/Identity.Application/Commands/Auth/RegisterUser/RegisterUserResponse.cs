namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RegisterUser;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    bool EmailVerificationRequired);
