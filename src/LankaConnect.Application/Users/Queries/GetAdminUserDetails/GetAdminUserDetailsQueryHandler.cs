using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Queries.GetAdminUserDetails;

/// <summary>
/// Handler for GetAdminUserDetailsQuery
/// Phase 6A.90: Returns detailed user information for admin view
/// </summary>
public class GetAdminUserDetailsQueryHandler : IQueryHandler<GetAdminUserDetailsQuery, AdminUserDetailsDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAdminUserDetailsQueryHandler> _logger;

    public GetAdminUserDetailsQueryHandler(
        IUserRepository userRepository,
        ILogger<GetAdminUserDetailsQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AdminUserDetailsDto>> Handle(
        GetAdminUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetAdminUserDetails"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetAdminUserDetails START: UserId={UserId}",
                request.UserId);

            try
            {
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetAdminUserDetails FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<AdminUserDetailsDto>.Failure("User not found");
                }

                var now = DateTime.UtcNow;
                var dto = new AdminUserDetailsDto
                {
                    UserId = user.Id,
                    Email = user.Email.Value,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber?.Value,
                    Bio = user.Bio,
                    Role = user.Role.ToString(),
                    IdentityProvider = user.IdentityProvider.ToString(),
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    IsAccountLocked = user.AccountLockedUntil.HasValue && user.AccountLockedUntil > now,
                    AccountLockedUntil = user.AccountLockedUntil,
                    FailedLoginAttempts = user.FailedLoginAttempts,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    ProfilePhotoUrl = user.ProfilePhotoUrl,
                    PendingUpgradeRole = user.PendingUpgradeRole?.ToString(),
                    UpgradeRequestedAt = user.UpgradeRequestedAt,
                    Location = user.Location != null ? new UserLocationDto
                    {
                        City = user.Location.City,
                        State = user.Location.State,
                        Country = user.Location.Country
                    } : null
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetAdminUserDetails COMPLETE: UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    user.Id, user.Email.Value, stopwatch.ElapsedMilliseconds);

                return Result<AdminUserDetailsDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetAdminUserDetails FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
