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
/// Tests for User.UpdateLanguages() method
/// Follows architect guidance: 1-5 languages required (min 1, max 5)
/// </summary>
public class UserUpdateLanguagesTests
{
    private Result<User> CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email.Value, "John", "Doe");
    }

    [Fact]
    public void UpdateLanguages_Should_Add_Languages_Successfully()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var languages = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value
        };

        // Act
        var result = user.UpdateLanguages(languages);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Languages.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateLanguages_Should_Replace_Existing_Languages()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var initial = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value
        };
        user.UpdateLanguages(initial);

        var newLanguages = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Tamil, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Intermediate).Value
        };

        // Act
        var result = user.UpdateLanguages(newLanguages);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Languages.Should().HaveCount(2);
        user.Languages.Should().Contain(l => l.Language == LanguageCode.Tamil);
        user.Languages.Should().NotContain(l => l.Language == LanguageCode.Sinhala);
    }

    [Fact]
    public void UpdateLanguages_Should_Fail_When_Empty()
    {
        // Arrange
        var user = CreateTestUser().Value;

        // Act
        var result = user.UpdateLanguages(new List<LanguagePreference>());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least 1");
    }

    [Fact]
    public void UpdateLanguages_Should_Fail_When_More_Than_5()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var tooMany = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value,
            LanguagePreference.Create(LanguageCode.Tamil, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.Hindi, ProficiencyLevel.Intermediate).Value,
            LanguagePreference.Create(LanguageCode.Bengali, ProficiencyLevel.Basic).Value,
            LanguagePreference.Create(LanguageCode.Urdu, ProficiencyLevel.Basic).Value
        };

        // Act
        var result = user.UpdateLanguages(tooMany);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("5");
    }

    [Fact]
    public void UpdateLanguages_Should_Allow_Exactly_5()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var five = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value,
            LanguagePreference.Create(LanguageCode.Tamil, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.Hindi, ProficiencyLevel.Intermediate).Value,
            LanguagePreference.Create(LanguageCode.Bengali, ProficiencyLevel.Basic).Value
        };

        // Act
        var result = user.UpdateLanguages(five);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Languages.Should().HaveCount(5);
    }

    [Fact]
    public void UpdateLanguages_Should_Remove_Duplicate_Languages()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var withDuplicates = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value
        };

        // Act
        var result = user.UpdateLanguages(withDuplicates);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.Languages.Should().HaveCount(1);
    }

    [Fact]
    public void UpdateLanguages_Should_Allow_Same_Language_Different_Proficiency()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var languages = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Basic).Value
        };

        // Act
        var result = user.UpdateLanguages(languages);

        // Assert - should keep both since proficiency differs
        result.IsSuccess.Should().BeTrue();
        user.Languages.Should().HaveCount(2);
    }

    [Fact]
    public void UpdateLanguages_Should_Raise_Domain_Event()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents();
        var languages = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Sinhala, ProficiencyLevel.Native).Value
        };

        // Act
        user.UpdateLanguages(languages);

        // Assert
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<LanguagesUpdatedEvent>();
    }

    [Fact]
    public void UpdateLanguages_Event_Should_Contain_User_Id_And_Languages()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents();
        var languages = new List<LanguagePreference>
        {
            LanguagePreference.Create(LanguageCode.Tamil, ProficiencyLevel.Advanced).Value,
            LanguagePreference.Create(LanguageCode.English, ProficiencyLevel.Intermediate).Value
        };

        // Act
        user.UpdateLanguages(languages);

        // Assert
        var domainEvent = user.DomainEvents.First() as LanguagesUpdatedEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(user.Id);
        domainEvent.Languages.Should().HaveCount(2);
    }
}
