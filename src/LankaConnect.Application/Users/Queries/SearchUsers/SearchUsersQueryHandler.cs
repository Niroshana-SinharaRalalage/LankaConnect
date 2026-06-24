using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Users.Queries.SearchUsers;

/// <summary>
/// Phase 6A.133: Handles user search for co-organizer linking.
/// Searches by name, email, or phone. Returns max 10 results.
/// Excludes the current user from results.
/// </summary>
public class SearchUsersQueryHandler : IQueryHandler<SearchUsersQuery, IReadOnlyList<UserSearchResultDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SearchUsersQueryHandler> _logger;

    public SearchUsersQueryHandler(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        ILogger<SearchUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<UserSearchResultDto>>> Handle(
        SearchUsersQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SearchUsers"))
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    return Result<IReadOnlyList<UserSearchResultDto>>.Failure(
                        "Search term is required");
                }

                if (request.SearchTerm.Trim().Length < 2)
                {
                    return Result<IReadOnlyList<UserSearchResultDto>>.Failure(
                        "Search term must be at least 2 characters");
                }

                var currentUserId = _currentUserService.UserId;

                _logger.LogInformation(
                    "SearchUsers START: Term={SearchTerm}, ExcludeUserId={ExcludeUserId}",
                    request.SearchTerm, currentUserId);

                var users = await _userRepository.SearchUsersAsync(
                    request.SearchTerm,
                    excludeUserId: currentUserId != Guid.Empty ? currentUserId : null,
                    maxResults: 10,
                    cancellationToken);

                var results = users.Select(u => new UserSearchResultDto
                {
                    Id = u.Id,
                    DisplayName = u.FullName,
                    Email = u.Email.Value,
                    ProfilePhotoUrl = u.ProfilePhotoUrl
                }).ToList().AsReadOnly();

                stopwatch.Stop();

                _logger.LogInformation(
                    "SearchUsers COMPLETE: Term={SearchTerm}, ResultCount={ResultCount}, Duration={ElapsedMs}ms",
                    request.SearchTerm, results.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<UserSearchResultDto>>.Success(results);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "SearchUsers FAILED: Term={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.SearchTerm, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
