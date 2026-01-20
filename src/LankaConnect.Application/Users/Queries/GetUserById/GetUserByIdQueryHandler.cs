using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetUserById"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetUserById START: UserId={UserId}",
                request.UserId);

            try
            {
                // Validate request
                if (request.UserId == Guid.Empty)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserById FAILED: Invalid UserId - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UserDto>.Failure("User ID is required");
                }

                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetUserById FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<UserDto>.Failure("User not found");
                }

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

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetUserById COMPLETE: UserId={UserId}, IsActive={IsActive}, HasLocation={HasLocation}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    request.UserId, user.IsActive, user.Location != null, user.PreferredMetroAreaIds.Count, stopwatch.ElapsedMilliseconds);

                return Result<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetUserById FAILED: Exception occurred - UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}