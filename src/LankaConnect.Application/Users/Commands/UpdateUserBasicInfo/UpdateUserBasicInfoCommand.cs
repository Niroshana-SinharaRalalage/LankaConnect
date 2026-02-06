using MediatR;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Users.Commands.UpdateUserBasicInfo;

/// <summary>
/// Command to update user's basic information (Name, Phone, Bio)
/// Phase 6A.70: Profile Basic Info Section
///
/// Does NOT include email changes - see UpdateUserEmailCommand for email updates
/// </summary>
public record UpdateUserBasicInfoCommand : IRequest<Result<UserDto>>
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
}
