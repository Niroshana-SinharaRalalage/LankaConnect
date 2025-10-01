using MediatR;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Auth.Commands.RegisterUser;

public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role = UserRole.User) : IRequest<Result<RegisterUserResponse>>;