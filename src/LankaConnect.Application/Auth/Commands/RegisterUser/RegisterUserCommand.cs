using MediatR;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Auth.Commands.RegisterUser;

/// <summary>
/// Register User Command
/// Phase 6A.0: Added SelectedRole parameter for role-based registration
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole? SelectedRole = null, // Phase 6A.0: Optional role selection (defaults to GeneralUser)
    List<Guid>? PreferredMetroAreaIds = null) : IRequest<Result<RegisterUserResponse>>; // Phase 5A: Optional metro areas