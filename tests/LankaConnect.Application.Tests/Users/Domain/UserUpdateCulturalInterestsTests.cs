using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// Tests for User.UpdateCulturalInterests() method
/// Follows architect guidance: 0-10 interests allowed, empty collection clears interests
/// </summary>
public class UserUpdateCulturalInterestsTests
{
    private Result<User> CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email.Value, "John", "Doe");
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Add_Interests_Successfully()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var interests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.CricketCulture,
            CulturalInterest.VesakCelebrations
        };

        // Act
        var result = user.UpdateCulturalInterests(interests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(3);
        user.CulturalInterests.Should().Contain(CulturalInterest.SriLankanCuisine);
        user.CulturalInterests.Should().Contain(CulturalInterest.CricketCulture);
        user.CulturalInterests.Should().Contain(CulturalInterest.VesakCelebrations);
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Replace_Existing_Interests()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialInterests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.CricketCulture
        };
        user.UpdateCulturalInterests(initialInterests);

        var newInterests = new List<CulturalInterest>
        {
            CulturalInterest.VesakCelebrations,
            CulturalInterest.TeaCulture
        };

        // Act
        var result = user.UpdateCulturalInterests(newInterests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(2);
        user.CulturalInterests.Should().Contain(CulturalInterest.VesakCelebrations);
        user.CulturalInterests.Should().Contain(CulturalInterest.TeaCulture);
        user.CulturalInterests.Should().NotContain(CulturalInterest.SriLankanCuisine);
        user.CulturalInterests.Should().NotContain(CulturalInterest.CricketCulture);
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Allow_Empty_Collection()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialInterests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.CricketCulture
        };
        user.UpdateCulturalInterests(initialInterests);

        // Act
        var result = user.UpdateCulturalInterests(new List<CulturalInterest>());

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().BeEmpty("users can clear all interests for privacy");
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Remove_Duplicates()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var interestsWithDuplicates = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.CricketCulture,
            CulturalInterest.SriLankanCuisine // Duplicate
        };

        // Act
        var result = user.UpdateCulturalInterests(interestsWithDuplicates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(2, "duplicates should be removed");
        user.CulturalInterests.Should().Contain(CulturalInterest.SriLankanCuisine);
        user.CulturalInterests.Should().Contain(CulturalInterest.CricketCulture);
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Accept_More_Than_10_Interests()
    {
        // Arrange - Phase 6A.47: Removed 10-interest limit, now unlimited
        var user = CreateTestUser().Value;
        var manyInterests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.BuddhistFestivals,
            CulturalInterest.HinduFestivals,
            CulturalInterest.IslamicFestivals,
            CulturalInterest.ChristianFestivals,
            CulturalInterest.TraditionalDance,
            CulturalInterest.CricketCulture,
            CulturalInterest.AyurvedicWellness,
            CulturalInterest.SinhalaMusic,
            CulturalInterest.TamilMusic,
            CulturalInterest.VesakCelebrations, // 11th interest - should be allowed
            CulturalInterest.FromCode("Business").Value, // 12th - dynamic code
            CulturalInterest.FromCode("Cultural").Value  // 13th - dynamic code
        };

        // Act
        var result = user.UpdateCulturalInterests(manyInterests);

        // Assert - Should succeed with 13 interests
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(13);
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Allow_Exactly_10_Interests()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var tenInterests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.BuddhistFestivals,
            CulturalInterest.HinduFestivals,
            CulturalInterest.IslamicFestivals,
            CulturalInterest.ChristianFestivals,
            CulturalInterest.TraditionalDance,
            CulturalInterest.CricketCulture,
            CulturalInterest.AyurvedicWellness,
            CulturalInterest.SinhalaMusic,
            CulturalInterest.TamilMusic
        };

        // Act
        var result = user.UpdateCulturalInterests(tenInterests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.CulturalInterests.Should().HaveCount(10);
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Raise_Domain_Event()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents(); // Clear creation event
        var interests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.CricketCulture
        };

        // Act
        var result = user.UpdateCulturalInterests(interests);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<CulturalInterestsUpdatedEvent>();
    }

    [Fact]
    public void UpdateCulturalInterests_Should_Not_Raise_Event_When_Clearing_Interests()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initialInterests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine
        };
        user.UpdateCulturalInterests(initialInterests);
        user.ClearDomainEvents(); // Clear previous events

        // Act
        var result = user.UpdateCulturalInterests(new List<CulturalInterest>());

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.DomainEvents.Should().BeEmpty("clearing interests is a privacy action, no event needed");
    }

    [Fact]
    public void UpdateCulturalInterests_Event_Should_Contain_User_Id_And_Interests()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents();
        var interests = new List<CulturalInterest>
        {
            CulturalInterest.SriLankanCuisine,
            CulturalInterest.VesakCelebrations
        };

        // Act
        user.UpdateCulturalInterests(interests);

        // Assert
        var domainEvent = user.DomainEvents.First() as CulturalInterestsUpdatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.Interests.Should().HaveCount(2);
        domainEvent.Interests.Should().Contain(CulturalInterest.SriLankanCuisine);
        domainEvent.Interests.Should().Contain(CulturalInterest.VesakCelebrations);
    }
}
