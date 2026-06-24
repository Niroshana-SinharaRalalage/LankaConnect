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
    public void IIdentityQueries_Has_FourteenMethods()
    {
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
    public void IIdentityCommands_Has_TwoSemanticPasswordResetMethods()
    {
        var methods = typeof(IIdentityCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "InitiatePasswordResetAsync",
            "CompletePasswordResetAsync",
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

        ((byte)UserRoleDto.Member).Should().Be(0);
        ((byte)UserRoleDto.Organizer).Should().Be(1);
        ((byte)UserRoleDto.Admin).Should().Be(2);
        ((byte)UserRoleDto.SuperAdmin).Should().Be(3);
        ((byte)UserRoleDto.Volunteer).Should().Be(4);
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
        // to Identity.Contracts; IJwtTokenService stays in legacy
        // LankaConnect.Application.Common.Interfaces (signature takes User type).
        typeof(ICurrentUserService).Namespace.Should().Be("LankaConnect.Modules.Identity.Contracts");
    }
}
