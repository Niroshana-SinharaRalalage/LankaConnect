using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Users.Commands.CreateUser;

public record CreateUserCommand : ICommand<Guid>
{
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
}