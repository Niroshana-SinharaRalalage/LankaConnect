using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
            return Result<UserDto>.Failure("User not found");

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber?.Value,
            Bio = user.Bio,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Result<UserDto>.Success(userDto);
    }
}