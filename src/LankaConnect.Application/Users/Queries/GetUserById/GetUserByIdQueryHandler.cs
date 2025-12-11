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
            UpdatedAt = user.UpdatedAt,

            // Epic 1 Phase 3: Profile Enhancement Fields
            ProfilePhotoUrl = user.ProfilePhotoUrl,
            Location = user.Location != null ? new UserLocationDto
            {
                City = user.Location.City,
                State = user.Location.State,
                ZipCode = user.Location.ZipCode,
                Country = user.Location.Country
            } : null,
            CulturalInterests = user.CulturalInterests.Select(ci => ci.Code).ToList(),
            Languages = user.Languages.Select(l => new LanguageDto
            {
                LanguageCode = l.Language.Code,
                ProficiencyLevel = l.Proficiency
            }).ToList(),

            // Phase 5B/6A.9: User Preferred Metro Areas (0-20 GUIDs)
            // CRITICAL FIX: Map PreferredMetroAreaIds from domain to DTO
            PreferredMetroAreas = user.PreferredMetroAreaIds.ToList()
        };

        return Result<UserDto>.Success(userDto);
    }
}