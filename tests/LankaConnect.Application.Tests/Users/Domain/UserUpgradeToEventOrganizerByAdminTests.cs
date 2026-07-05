using FluentAssertions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Modules.Identity.Domain.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// Phase 6A.139 Slice 1: TDD tests for User.UpgradeToEventOrganizerByAdmin().
/// Mirrors DowngradeToGeneralUserByAdmin() invariants in the inverse direction.
/// Written FIRST (Red) before the domain method exists.
/// </summary>
public class UserUpgradeToEventOrganizerByAdminTests
{
    private static User CreateUser(UserRole role = UserRole.GeneralUser)
    {
        var email = Email.Create("upgrade.target@test.com").Value;
        var user = User.Create(email, "Upgrade", "Target").Value;

        // Reflection: Role has a private setter
        typeof(User).GetProperty(nameof(User.Role))!.SetValue(user, role);
        return user;
    }

    [Fact]
    public void UpgradeToEventOrganizerByAdmin_FromGeneralUser_ShouldSucceed()
    {
        var user = CreateUser(UserRole.GeneralUser);

        var result = user.UpgradeToEventOrganizerByAdmin();

        result.IsSuccess.Should().BeTrue();
        user.Role.Should().Be(UserRole.EventOrganizer);
    }

    [Fact]
    public void UpgradeToEventOrganizerByAdmin_FromGeneralUser_ShouldRaiseUserRoleChangedEvent()
    {
        var user = CreateUser(UserRole.GeneralUser);
        user.ClearDomainEvents();

        var result = user.UpgradeToEventOrganizerByAdmin();

        result.IsSuccess.Should().BeTrue();
        var domainEvent = user.DomainEvents.OfType<UserRoleChangedEvent>().Single();
        domainEvent.UserId.Should().Be(user.Id);
        domainEvent.OldRole.Should().Be(UserRole.GeneralUser);
        domainEvent.NewRole.Should().Be(UserRole.EventOrganizer);
        domainEvent.Email.Should().Be(user.Email.Value);
    }

    [Fact]
    public void UpgradeToEventOrganizerByAdmin_AlreadyEventOrganizer_ShouldFail()
    {
        var user = CreateUser(UserRole.EventOrganizer);

        var result = user.UpgradeToEventOrganizerByAdmin();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already an Event Organizer");
        user.Role.Should().Be(UserRole.EventOrganizer);
    }

    [Theory]
    [InlineData(UserRole.BusinessOwner)]
    [InlineData(UserRole.EventOrganizerAndBusinessOwner)]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.AdminManager)]
    public void UpgradeToEventOrganizerByAdmin_FromNonGeneralUserRole_ShouldFail(UserRole startingRole)
    {
        var user = CreateUser(startingRole);

        var result = user.UpgradeToEventOrganizerByAdmin();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only General Users can be upgraded");
        user.Role.Should().Be(startingRole);
    }

    [Fact]
    public void UpgradeToEventOrganizerByAdmin_WithPendingUpgradeRequest_ShouldClearPendingFields()
    {
        var user = CreateUser(UserRole.GeneralUser);
        // Simulate pending upgrade
        typeof(User).GetProperty(nameof(User.PendingUpgradeRole))!.SetValue(user, (UserRole?)UserRole.EventOrganizer);
        typeof(User).GetProperty(nameof(User.UpgradeRequestedAt))!.SetValue(user, (DateTime?)DateTime.UtcNow);

        var result = user.UpgradeToEventOrganizerByAdmin();

        result.IsSuccess.Should().BeTrue();
        user.PendingUpgradeRole.Should().BeNull();
        user.UpgradeRequestedAt.Should().BeNull();
    }

    [Fact]
    public void UpgradeToEventOrganizerByAdmin_ShouldUpdateUpdatedAtTimestamp()
    {
        var user = CreateUser(UserRole.GeneralUser);
        var before = user.UpdatedAt;

        // Ensure timestamp tick advances on systems with low clock resolution
        System.Threading.Thread.Sleep(5);

        var result = user.UpgradeToEventOrganizerByAdmin();

        result.IsSuccess.Should().BeTrue();
        user.UpdatedAt.Should().NotBe(before);
    }
}
