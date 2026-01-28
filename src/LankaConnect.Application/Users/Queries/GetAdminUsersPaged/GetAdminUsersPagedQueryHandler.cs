using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Queries.GetAdminUsersPaged;

/// <summary>
/// Handler for GetAdminUsersPagedQuery
/// Phase 6A.90: Returns paginated list of users for admin management
/// </summary>
public class GetAdminUsersPagedQueryHandler : IQueryHandler<GetAdminUsersPagedQuery, PagedResultDto<AdminUserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAdminUsersPagedQueryHandler> _logger;

    public GetAdminUsersPagedQueryHandler(
        IUserRepository userRepository,
        ILogger<GetAdminUsersPagedQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResultDto<AdminUserDto>>> Handle(
        GetAdminUsersPagedQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetAdminUsersPaged"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Page", request.Page))
        using (LogContext.PushProperty("PageSize", request.PageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetAdminUsersPaged START: Page={Page}, PageSize={PageSize}, SearchTerm={SearchTerm}, RoleFilter={RoleFilter}, IsActiveFilter={IsActiveFilter}",
                request.Page, request.PageSize, request.SearchTerm, request.RoleFilter, request.IsActiveFilter);

            try
            {
                // Validate pagination parameters
                var page = Math.Max(1, request.Page);
                var pageSize = Math.Clamp(request.PageSize, 1, 100);

                var (users, totalCount) = await _userRepository.GetPagedAsync(
                    page,
                    pageSize,
                    request.SearchTerm,
                    request.RoleFilter,
                    request.IsActiveFilter,
                    cancellationToken);

                _logger.LogInformation(
                    "GetAdminUsersPaged: Users loaded - ItemCount={ItemCount}, TotalCount={TotalCount}",
                    users.Count, totalCount);

                var now = DateTime.UtcNow;
                var dtos = users.Select(user => new AdminUserDto
                {
                    UserId = user.Id,
                    Email = user.Email.Value,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Role = user.Role.ToString(),
                    IdentityProvider = user.IdentityProvider.ToString(),
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    IsAccountLocked = user.AccountLockedUntil.HasValue && user.AccountLockedUntil > now,
                    AccountLockedUntil = user.AccountLockedUntil,
                    LastLoginAt = user.LastLoginAt,
                    CreatedAt = user.CreatedAt,
                    ProfilePhotoUrl = user.ProfilePhotoUrl
                }).ToList();

                var result = new PagedResultDto<AdminUserDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                };

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetAdminUsersPaged COMPLETE: Page={Page}, ItemCount={ItemCount}, TotalCount={TotalCount}, TotalPages={TotalPages}, Duration={ElapsedMs}ms",
                    page, dtos.Count, totalCount, result.TotalPages, stopwatch.ElapsedMilliseconds);

                return Result<PagedResultDto<AdminUserDto>>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetAdminUsersPaged FAILED: Page={Page}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Page, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
