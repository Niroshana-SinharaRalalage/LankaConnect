using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using LankaConnect.Modules.Identity.Application.Mappings;
using LankaConnect.Modules.Identity.Contracts;

namespace LankaConnect.Modules.Identity.Application.Queries;

/// <summary>
/// Implementation of <see cref="IIdentityQueries"/>. Wave 4.6.b (2026-06-24).
/// Thin adapter over the legacy <see cref="IUserRepository"/> that projects
/// User aggregates to Identity.Contracts DTOs.
/// </summary>
/// <remarks>
/// The wrapped repository still lives in <c>LankaConnect.Domain.Users</c> until
/// Wave 4.6.d.2 (physical move). The transitional
/// <c>ProjectReference LankaConnect.Domain</c> in Identity.Application.csproj is
/// what makes this wiring compile during the transition window. Cut at 4.6.d.2.
/// </remarks>
public sealed class IdentityQueries : IIdentityQueries
{
    private readonly IUserRepository _userRepository;

    public IdentityQueries(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserSummaryDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        return user?.ToSummaryDto();
    }

    public async Task<UserDetailDto?> GetUserDetailAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        return user?.ToDetailDto();
    }

    public async Task<UserSummaryDto?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        if (emailVo.IsFailure) return null;

        var user = await _userRepository.GetByEmailAsync(emailVo.Value, ct);
        return user?.ToSummaryDto();
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetByIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0) return Array.Empty<UserSummaryDto>();

        // No batch-id method on IUserRepository today; fan out + filter nulls.
        // 4.6.d.1 audit can add IUserRepository.GetByIdsAsync if N+1 surfaces.
        var result = new List<UserSummaryDto>(ids.Count);
        foreach (var id in ids)
        {
            var user = await _userRepository.GetByIdAsync(id, ct);
            if (user is not null) result.Add(user.ToSummaryDto());
        }
        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        return await _userRepository.GetUserNamesAsync(ids, ct);
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetEmailsByUserIdsAsync(
        IReadOnlyList<Guid> ids,
        CancellationToken ct = default)
    {
        if (ids.Count == 0) return new Dictionary<Guid, string>();
        return await _userRepository.GetEmailsByUserIdsAsync(ids, ct);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> SearchByNameAsync(
        string term,
        CancellationToken ct = default)
    {
        var users = await _userRepository.SearchByNameAsync(term, ct);
        var result = new List<UserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(user.ToSummaryDto());
        }
        return result;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> SearchUsersAsync(
        string searchTerm,
        Guid? excludeUserId,
        int maxResults,
        CancellationToken ct = default)
    {
        var users = await _userRepository.SearchUsersAsync(searchTerm, excludeUserId, maxResults, ct);
        var result = new List<UserSummaryDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(user.ToSummaryDto());
        }
        return result;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetUserSummariesByEmailsAsync(
        IReadOnlyList<string> emails,
        CancellationToken ct = default)
    {
        if (emails.Count == 0) return Array.Empty<UserSummaryDto>();
        var result = new List<UserSummaryDto>(emails.Count);
        foreach (var email in emails)
        {
            var emailVo = Email.Create(email);
            if (emailVo.IsFailure) continue;
            var user = await _userRepository.GetByEmailAsync(emailVo.Value, ct);
            if (user is not null) result.Add(user.ToSummaryDto());
        }
        return result;
    }

    public async Task<UserContactDto?> GetContactInfoAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        return user?.ToContactDto();
    }

    public async Task<UserPreferencesProjectionDto?> GetPreferencesAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user is null) return null;
        return new UserPreferencesProjectionDto(
            UserId: user.Id,
            PreferredMetroAreaIds: user.PreferredMetroAreaIds.ToList(),
            LocationCity: user.Location?.City,
            LocationState: user.Location?.State);
    }

    public async Task<(IReadOnlyList<UserSummaryDto> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm,
        UserRoleDto? roleFilter,
        bool? activeFilter,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _userRepository.GetPagedAsync(
            page, pageSize, searchTerm,
            roleFilter is null ? null : (UserRole?)(byte)roleFilter.Value,
            activeFilter, ct);

        var dtos = new List<UserSummaryDto>(items.Count);
        foreach (var user in items)
        {
            dtos.Add(user.ToSummaryDto());
        }
        return (dtos, totalCount);
    }

    public async Task<IReadOnlyList<UserPendingRoleUpgradeDto>> GetUsersWithPendingRoleUpgradesAsync(
        CancellationToken ct = default)
    {
        var users = await _userRepository.GetUsersWithPendingRoleUpgradesAsync(ct);
        var result = new List<UserPendingRoleUpgradeDto>(users.Count);
        foreach (var user in users)
        {
            result.Add(user.ToPendingRoleUpgradeDto());
        }
        return result;
    }

    public Task<int> CountAsync(CancellationToken ct = default) =>
        _userRepository.CountAsync(ct);

    public Task<int> CountActiveUsersAsync(CancellationToken ct = default) =>
        _userRepository.GetActiveUsersCountAsync(ct);

    public Task<int> CountLockedAccountsAsync(CancellationToken ct = default) =>
        _userRepository.GetLockedAccountsCountAsync(ct);

    public async Task<IReadOnlyDictionary<UserRoleDto, int>> GetUserCountsByRoleAsync(
        CancellationToken ct = default)
    {
        var domainMap = await _userRepository.GetUserCountsByRoleAsync(ct);
        var result = new Dictionary<UserRoleDto, int>(domainMap.Count);
        foreach (var kvp in domainMap)
        {
            result[(UserRoleDto)(byte)kvp.Key] = kvp.Value;
        }
        return result;
    }

    public async Task<bool> ExistsWithEmailAsync(string email, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        if (emailVo.IsFailure) return false;
        return await _userRepository.ExistsWithEmailAsync(emailVo.Value, ct);
    }
}
