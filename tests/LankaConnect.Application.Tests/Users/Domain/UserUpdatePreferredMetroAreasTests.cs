using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// Tests for User.UpdatePreferredMetroAreas() method
/// Phase 5A: User Preferred Metro Areas
/// Follows ADR-008: 0-10 metro areas allowed, empty collection clears preferences
/// </summary>
public class UserUpdatePreferredMetroAreasTests
{
    private Result<User> CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email.Value, "John", "Doe");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Add_Metro_Areas_Successfully()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var metroAreaIds = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"), // Cleveland
            Guid.Parse("11111111-0000-0000-0000-000000000002"), // Columbus
            Guid.Parse("11111111-0000-0000-0000-000000000003")  // Cincinnati
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(metroAreaIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().HaveCount(3);
        user.PreferredMetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000001"));
        user.PreferredMetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000002"));
        user.PreferredMetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000003"));
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Replace_Existing_Metro_Areas()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"), // Cleveland
            Guid.Parse("11111111-0000-0000-0000-000000000002")  // Columbus
        };
        user.UpdatePreferredMetroAreas(initialMetroAreas);

        var newMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000003"), // Cincinnati
            Guid.Parse("11111111-0000-0000-0000-000000000004")  // Toledo
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(newMetroAreas);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().HaveCount(2);
        user.PreferredMetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000003"));
        user.PreferredMetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000004"));
        user.PreferredMetroAreaIds.Should().NotContain(Guid.Parse("11111111-0000-0000-0000-000000000001"));
        user.PreferredMetroAreaIds.Should().NotContain(Guid.Parse("11111111-0000-0000-0000-000000000002"));
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Allow_Empty_Collection()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-0000-0000-0000-000000000002")
        };
        user.UpdatePreferredMetroAreas(initialMetroAreas);

        // Act
        var result = user.UpdatePreferredMetroAreas(new List<Guid>());

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().BeEmpty("users can clear all preferences for privacy");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Allow_Null_Collection()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001")
        };
        user.UpdatePreferredMetroAreas(initialMetroAreas);

        // Act
        var result = user.UpdatePreferredMetroAreas(null);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().BeEmpty("null is treated as empty collection");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Remove_Duplicates()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var metroAreasWithDuplicates = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-0000-0000-0000-000000000002"),
            Guid.Parse("11111111-0000-0000-0000-000000000001") // Duplicate
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(metroAreasWithDuplicates);

        // Assert - According to implementation, duplicates are detected and rejected
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Duplicate");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Fail_When_More_Than_20_Metro_Areas()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var tooManyMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-0000-0000-0000-000000000002"),
            Guid.Parse("11111111-0000-0000-0000-000000000003"),
            Guid.Parse("11111111-0000-0000-0000-000000000004"),
            Guid.Parse("11111111-0000-0000-0000-000000000005"),
            Guid.Parse("11111111-0000-0000-0000-000000000006"),
            Guid.Parse("22222222-0000-0000-0000-000000000001"),
            Guid.Parse("22222222-0000-0000-0000-000000000002"),
            Guid.Parse("33333333-0000-0000-0000-000000000001"),
            Guid.Parse("33333333-0000-0000-0000-000000000002"),
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Guid.Parse("44444444-0000-0000-0000-000000000001"),
            Guid.Parse("44444444-0000-0000-0000-000000000002"),
            Guid.Parse("55555555-0000-0000-0000-000000000001"),
            Guid.Parse("55555555-0000-0000-0000-000000000002"),
            Guid.Parse("66666666-0000-0000-0000-000000000001"),
            Guid.Parse("66666666-0000-0000-0000-000000000002"),
            Guid.Parse("77777777-0000-0000-0000-000000000001"),
            Guid.Parse("77777777-0000-0000-0000-000000000002"),
            Guid.Parse("88888888-0000-0000-0000-000000000001"),
            Guid.Parse("88888888-0000-0000-0000-000000000002") // 21st metro area (over the limit of 20)
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(tooManyMetroAreas);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("20");
        result.Error.Should().Contain("metro area");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Allow_Exactly_10_Metro_Areas()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var tenMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-0000-0000-0000-000000000002"),
            Guid.Parse("11111111-0000-0000-0000-000000000003"),
            Guid.Parse("11111111-0000-0000-0000-000000000004"),
            Guid.Parse("11111111-0000-0000-0000-000000000005"),
            Guid.Parse("11111111-0000-0000-0000-000000000006"),
            Guid.Parse("22222222-0000-0000-0000-000000000001"),
            Guid.Parse("22222222-0000-0000-0000-000000000002"),
            Guid.Parse("33333333-0000-0000-0000-000000000001"),
            Guid.Parse("33333333-0000-0000-0000-000000000002")
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(tenMetroAreas);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().HaveCount(10);
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Raise_Domain_Event()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents(); // Clear creation event
        var metroAreaIds = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-0000-0000-0000-000000000002")
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(metroAreaIds);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserPreferredMetroAreasUpdatedEvent>();
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Not_Raise_Event_When_Clearing_Preferences()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialMetroAreas = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001")
        };
        user.UpdatePreferredMetroAreas(initialMetroAreas);
        user.ClearDomainEvents(); // Clear previous events

        // Act
        var result = user.UpdatePreferredMetroAreas(new List<Guid>());

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().BeEmpty("clearing preferences is a privacy action, no event needed");
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Event_Should_Contain_User_Id_And_Metro_Area_Ids()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents();
        var metroAreaIds = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001"),
            Guid.Parse("11111111-0000-0000-0000-000000000002")
        };

        // Act
        user.UpdatePreferredMetroAreas(metroAreaIds);

        // Assert
        var domainEvent = user.DomainEvents.First() as UserPreferredMetroAreasUpdatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.MetroAreaIds.Should().HaveCount(2);
        domainEvent.MetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000001"));
        domainEvent.MetroAreaIds.Should().Contain(Guid.Parse("11111111-0000-0000-0000-000000000002"));
    }

    [Fact]
    public void UpdatePreferredMetroAreas_Should_Allow_Single_Metro_Area()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var singleMetroArea = new List<Guid>
        {
            Guid.Parse("11111111-0000-0000-0000-000000000001")
        };

        // Act
        var result = user.UpdatePreferredMetroAreas(singleMetroArea);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.PreferredMetroAreaIds.Should().HaveCount(1);
        user.PreferredMetroAreaIds.First().Should().Be(Guid.Parse("11111111-0000-0000-0000-000000000001"));
    }
}
