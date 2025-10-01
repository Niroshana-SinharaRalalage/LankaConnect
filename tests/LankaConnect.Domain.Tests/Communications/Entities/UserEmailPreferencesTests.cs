using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Tests.TestHelpers;

namespace LankaConnect.Domain.Tests.Communications.Entities;

public class UserEmailPreferencesTests
{
    private readonly Guid _validUserId = Guid.NewGuid();

    #region Creation Tests

    [Fact]
    public void Create_WithValidUserId_ShouldReturnSuccess()
    {
        // Act
        var result = UserEmailPreferences.Create(_validUserId);

        // Assert
        Assert.True(result.IsSuccess);
        var preferences = result.Value;
        Assert.Equal(_validUserId, preferences.UserId);
        Assert.False(preferences.AllowMarketing); // Default should be false
        Assert.True(preferences.AllowNotifications); // Default should be true
        Assert.True(preferences.AllowNewsletters); // Default should be true
        Assert.True(preferences.AllowTransactional); // Default should be true
        Assert.Equal("en-US", preferences.PreferredLanguage); // Default language
        Assert.NotEqual(Guid.Empty, preferences.Id);
        Assert.True((DateTime.UtcNow - preferences.CreatedAt).TotalSeconds < 1);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldReturnFailure()
    {
        // Act
        var result = UserEmailPreferences.Create(Guid.Empty);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("User ID is required", result.Errors);
    }

    #endregion

    #region Marketing Preferences Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateMarketingPreference_WithValidValue_ShouldUpdateSuccessfully(bool allow)
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdateMarketingPreference(allow);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(allow, preferences.AllowMarketing);
        Assert.True(preferences.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateMarketingPreference_ShouldAllowOptingOut()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        preferences.UpdateMarketingPreference(true);

        // Act
        var result = preferences.UpdateMarketingPreference(false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(preferences.AllowMarketing);
    }

    [Fact]
    public void UpdateMarketingPreference_ShouldAllowOptingIn()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        Assert.False(preferences.AllowMarketing); // Default is false

        // Act
        var result = preferences.UpdateMarketingPreference(true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(preferences.AllowMarketing);
    }

    #endregion

    #region Notification Preferences Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateNotificationPreference_WithValidValue_ShouldUpdateSuccessfully(bool allow)
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdateNotificationPreference(allow);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(allow, preferences.AllowNotifications);
        Assert.True(preferences.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateNotificationPreference_ShouldAllowDisabling()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        Assert.True(preferences.AllowNotifications); // Default is true

        // Act
        var result = preferences.UpdateNotificationPreference(false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(preferences.AllowNotifications);
    }

    #endregion

    #region Newsletter Preferences Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateNewsletterPreference_WithValidValue_ShouldUpdateSuccessfully(bool allow)
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdateNewsletterPreference(allow);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(allow, preferences.AllowNewsletters);
        Assert.True(preferences.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateNewsletterPreference_ShouldAllowOptingOut()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        Assert.True(preferences.AllowNewsletters); // Default is true

        // Act
        var result = preferences.UpdateNewsletterPreference(false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(preferences.AllowNewsletters);
    }

    #endregion

    #region Transactional Email Tests (Critical Business Rule)

    [Fact]
    public void UpdateTransactionalPreference_WithTrue_ShouldUpdateSuccessfully()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdateTransactionalPreference(true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(preferences.AllowTransactional);
        Assert.True(preferences.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public void UpdateTransactionalPreference_WithFalse_ShouldReturnFailure()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalValue = preferences.AllowTransactional;
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdateTransactionalPreference(false);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Transactional emails cannot be disabled", result.Errors);
        Assert.Equal(originalValue, preferences.AllowTransactional); // Should not change
        Assert.Equal(originalUpdatedAt, preferences.UpdatedAt); // Should not update timestamp
    }

    [Fact]
    public void Create_ShouldAlwaysAllowTransactionalEmails()
    {
        // Act
        var result = UserEmailPreferences.Create(_validUserId);

        // Assert
        Assert.True(result.IsSuccess);
        var preferences = result.Value;
        Assert.True(preferences.AllowTransactional);
    }

    #endregion

    #region Language Preferences Tests

    [Theory]
    [InlineData("en-US")]
    [InlineData("si-LK")]
    [InlineData("ta-LK")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    public void UpdatePreferredLanguage_WithValidLanguage_ShouldUpdateSuccessfully(string language)
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdatePreferredLanguage(language);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(language, preferences.PreferredLanguage);
        Assert.True(preferences.UpdatedAt > originalUpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdatePreferredLanguage_WithInvalidLanguage_ShouldReturnFailure(string language)
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalLanguage = preferences.PreferredLanguage;
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act
        var result = preferences.UpdatePreferredLanguage(language);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Language is required", result.Errors);
        Assert.Equal(originalLanguage, preferences.PreferredLanguage); // Should not change
        Assert.Equal(originalUpdatedAt, preferences.UpdatedAt); // Should not update timestamp
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void UserEmailPreferences_MultipleUpdates_ShouldMaintainConsistentState()
    {
        // Arrange
        var preferences = CreateValidPreferences();

        // Act - Make multiple preference changes
        preferences.UpdateMarketingPreference(true);
        preferences.UpdateNotificationPreference(false);
        preferences.UpdateNewsletterPreference(false);
        preferences.UpdatePreferredLanguage("si-LK");

        // Assert - Verify final state
        Assert.True(preferences.AllowMarketing);
        Assert.False(preferences.AllowNotifications);
        Assert.False(preferences.AllowNewsletters);
        Assert.True(preferences.AllowTransactional); // Should always remain true
        Assert.Equal("si-LK", preferences.PreferredLanguage);
    }

    [Fact]
    public void UserEmailPreferences_ShouldInheritFromBaseEntity()
    {
        // Act
        var preferences = CreateValidPreferences();

        // Assert
        Assert.IsAssignableFrom<LankaConnect.Domain.Common.BaseEntity>(preferences);
        Assert.NotEqual(Guid.Empty, preferences.Id);
        Assert.True((DateTime.UtcNow - preferences.CreatedAt).TotalSeconds < 1);
        Assert.NotNull(preferences.UpdatedAt);
    }

    #endregion

    #region Business Rules Tests

    [Fact]
    public void Create_ShouldHaveConservativeDefaults()
    {
        // Act
        var result = UserEmailPreferences.Create(_validUserId);
        var preferences = result.Value;

        // Assert - Verify conservative defaults (opt-in rather than opt-out for marketing)
        Assert.False(preferences.AllowMarketing); // Conservative: requires explicit opt-in
        Assert.True(preferences.AllowNotifications); // Reasonable default for app functionality
        Assert.True(preferences.AllowNewsletters); // Reasonable default for platform updates
        Assert.True(preferences.AllowTransactional); // Required for security/account functionality
        Assert.Equal("en-US", preferences.PreferredLanguage); // Default to English
    }

    [Fact]
    public void TransactionalEmailRule_ShouldBeEnforcedConsistently()
    {
        // Arrange
        var preferences = CreateValidPreferences();

        // Act & Assert - Multiple attempts to disable should all fail
        var result1 = preferences.UpdateTransactionalPreference(false);
        var result2 = preferences.UpdateTransactionalPreference(false);
        var result3 = preferences.UpdateTransactionalPreference(false);

        Assert.True(result1.IsFailure);
        Assert.True(result2.IsFailure);
        Assert.True(result3.IsFailure);
        Assert.True(preferences.AllowTransactional); // Should never change
    }

    [Fact]
    public void UserEmailPreferences_ShouldHandleGDPRCompliance()
    {
        // Arrange & Act
        var preferences = CreateValidPreferences();

        // GDPR - User should be able to opt out of all non-essential communications
        preferences.UpdateMarketingPreference(false);
        preferences.UpdateNotificationPreference(false);
        preferences.UpdateNewsletterPreference(false);

        // Assert - Only transactional emails (essential for service) should remain enabled
        Assert.False(preferences.AllowMarketing);
        Assert.False(preferences.AllowNotifications);
        Assert.False(preferences.AllowNewsletters);
        Assert.True(preferences.AllowTransactional); // Cannot be disabled for security/legal reasons
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UpdatePreferredLanguage_WithWhitespaceLanguage_ShouldReturnFailure()
    {
        // Arrange
        var preferences = CreateValidPreferences();

        // Act
        var result = preferences.UpdatePreferredLanguage("   \t   ");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Language is required", result.Errors);
    }

    [Fact]
    public void UserEmailPreferences_UpdatedAt_ShouldBeUpdatedOnEveryChange()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act & Assert - Each preference update should update the timestamp
        System.Threading.Thread.Sleep(1); // Ensure time difference
        preferences.UpdateMarketingPreference(true);
        var firstUpdateTime = preferences.UpdatedAt;
        Assert.True(firstUpdateTime > originalUpdatedAt);

        System.Threading.Thread.Sleep(1);
        preferences.UpdateNotificationPreference(false);
        var secondUpdateTime = preferences.UpdatedAt;
        Assert.True(secondUpdateTime > firstUpdateTime);

        System.Threading.Thread.Sleep(1);
        preferences.UpdatePreferredLanguage("fr-FR");
        var thirdUpdateTime = preferences.UpdatedAt;
        Assert.True(thirdUpdateTime > secondUpdateTime);
    }

    [Fact]
    public void UserEmailPreferences_FailedUpdates_ShouldNotUpdateTimestamp()
    {
        // Arrange
        var preferences = CreateValidPreferences();
        var originalUpdatedAt = preferences.UpdatedAt;

        // Act - Attempt invalid updates
        System.Threading.Thread.Sleep(10);
        preferences.UpdateTransactionalPreference(false); // Should fail
        preferences.UpdatePreferredLanguage(""); // Should fail

        // Assert
        Assert.Equal(originalUpdatedAt, preferences.UpdatedAt);
    }

    #endregion

    #region Helper Methods

    private UserEmailPreferences CreateValidPreferences()
    {
        return UserEmailPreferences.Create(_validUserId).Value;
    }

    #endregion
}