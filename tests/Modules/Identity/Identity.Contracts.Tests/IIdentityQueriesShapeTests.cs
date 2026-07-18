using System.Reflection;
using LankaConnect.Modules.Identity.Contracts;

namespace LankaConnect.Modules.Identity.Contracts.Tests;

/// <summary>
/// Shape-pinning tests for <see cref="IIdentityQueries"/>, <see cref="IIdentityCommands"/>,
/// <see cref="ICurrentUserService"/>, and the Identity.Contracts DTOs/enums.
/// Wave 4.6.a (2026-06-24). Catches silent ABI drift -- if the cross-module surface
/// changes accidentally, the ~50 LankaConnect.Application + Communications/Notifications
/// consumers would break.
/// </summary>
public sealed class IIdentityQueriesShapeTests
{
    [Fact]
    public void IIdentityQueries_Has_EighteenMethods()
    {
        // W4.6.d.2.a (2026-06-24): expanded from 14 -> 17 with SearchUsersAsync,
        // GetUserSummariesByEmailsAsync, GetContactInfoAsync per architect ruling
        // to cover the d.2.b 44-consumer sweep.
        // W4.10.s1a (2026-06-26): added GetPreferencesAsync (#18) so
        // GetEventsQueryHandler + GetFeaturedEventsQueryHandler can swap from
        // IUserRepository to IIdentityQueries via UserPreferencesProjectionDto.
        var methods = typeof(IIdentityQueries).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "GetUserByIdAsync",
            "GetUserDetailAsync",
            "GetByEmailAsync",
            "GetByIdsAsync",
            "GetUserNamesAsync",
            "GetEmailsByUserIdsAsync",
            "SearchByNameAsync",
            "SearchUsersAsync",
            "GetUserSummariesByEmailsAsync",
            "GetContactInfoAsync",
            "GetPreferencesAsync",
            "GetPagedAsync",
            "GetUsersWithPendingRoleUpgradesAsync",
            "CountAsync",
            "CountActiveUsersAsync",
            "CountLockedAccountsAsync",
            "GetUserCountsByRoleAsync",
            "ExistsWithEmailAsync",
        });
    }

    [Fact]
    public void IIdentityCommands_Has_FourSemanticMutators()
    {
        // W4.6.d.2.a (2026-06-24): expanded from 2 -> 4 with email-verification
        // semantic mutators per architect ruling.
        var methods = typeof(IIdentityCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "InitiatePasswordResetAsync",
            "CompletePasswordResetAsync",
            "InitiateEmailVerificationAsync",
            "CompleteEmailVerificationAsync",
        });
    }

    [Fact]
    public void ICurrentUserService_HasFourProperties()
    {
        var props = typeof(ICurrentUserService).GetProperties();

        props.Select(p => p.Name).Should().BeEquivalentTo(new[]
        {
            nameof(ICurrentUserService.UserId),
            nameof(ICurrentUserService.UserEmail),
            nameof(ICurrentUserService.IsAuthenticated),
            nameof(ICurrentUserService.IsAdmin),
        });
    }

    [Fact]
    public void UserSummaryDto_Carries_CoreFields()
    {
        var props = typeof(UserSummaryDto).GetProperties();

        props.Select(p => p.Name).Should().BeEquivalentTo(new[]
        {
            nameof(UserSummaryDto.Id),
            nameof(UserSummaryDto.Email),
            nameof(UserSummaryDto.FirstName),
            nameof(UserSummaryDto.LastName),
            nameof(UserSummaryDto.DisplayName),
            nameof(UserSummaryDto.Role),
            nameof(UserSummaryDto.Status),
            nameof(UserSummaryDto.EmailVerified),
            nameof(UserSummaryDto.CreatedAt),
            nameof(UserSummaryDto.UpdatedAt),
        });
    }

    [Fact]
    public void UserRoleDto_Mirrors_DomainRole_ByteWidth()
    {
        Enum.GetUnderlyingType(typeof(UserRoleDto)).Should().Be(typeof(byte));

        ((byte)UserRoleDto.GeneralUser).Should().Be(1);
        ((byte)UserRoleDto.BusinessOwner).Should().Be(2);
        ((byte)UserRoleDto.EventOrganizer).Should().Be(3);
        ((byte)UserRoleDto.EventOrganizerAndBusinessOwner).Should().Be(4);
        ((byte)UserRoleDto.Admin).Should().Be(5);
        ((byte)UserRoleDto.AdminManager).Should().Be(6);
    }

    [Fact]
    public void UserStatusDto_IsByte()
    {
        Enum.GetUnderlyingType(typeof(UserStatusDto)).Should().Be(typeof(byte));
    }

    [Fact]
    public void PasswordResetInitiatedDto_Carries_FullEmailPayload()
    {
        var props = typeof(PasswordResetInitiatedDto).GetProperties();

        props.Select(p => p.Name).Should().BeEquivalentTo(new[]
        {
            nameof(PasswordResetInitiatedDto.UserId),
            nameof(PasswordResetInitiatedDto.Email),
            nameof(PasswordResetInitiatedDto.DisplayName),
            nameof(PasswordResetInitiatedDto.PasswordResetToken),
            nameof(PasswordResetInitiatedDto.TokenExpiresAt),
            nameof(PasswordResetInitiatedDto.WasThrottled),
        });
    }

    [Fact]
    public void ICurrentUserService_LivesIn_Identity_Contracts_Namespace()
    {
        // W4.6.a architect Risk #2 Option C ruling: ICurrentUserService moves
        // to Identity.Contracts.
        // Wave 8.5.a Part 1 (2026-07-17, D-12 Option b): IJwtTokenService +
        // IEntraExternalIdService now also live in Identity.Contracts.Services
        // after the User→AccessTokenClaims DTO reshape closed the Contracts↔
        // Domain leak that Risk #2 Option C originally deferred.
        typeof(ICurrentUserService).Namespace.Should().Be("LankaConnect.Modules.Identity.Contracts");
    }
}
