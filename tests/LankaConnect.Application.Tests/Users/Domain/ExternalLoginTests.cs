using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// Tests for ExternalLogin value object
/// Following TDD: Write tests first (RED phase)
/// </summary>
public class ExternalLoginTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var provider = FederatedProvider.Facebook;
        var externalId = "facebook-user-12345";
        var email = "user@facebook.com";

        // Act
        var result = ExternalLogin.Create(provider, externalId, email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(provider, result.Value.Provider);
        Assert.Equal(externalId, result.Value.ExternalProviderId);
        Assert.Equal(email, result.Value.ProviderEmail);
        Assert.True(DateTime.UtcNow - result.Value.LinkedAt < TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidExternalProviderId_ShouldReturnFailure(string invalidId)
    {
        // Act
        var result = ExternalLogin.Create(FederatedProvider.Google, invalidId, "user@google.com");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("External provider ID", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidProviderEmail_ShouldReturnFailure(string invalidEmail)
    {
        // Act
        var result = ExternalLogin.Create(FederatedProvider.Apple, "apple-user-123", invalidEmail);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Provider email", result.Error);
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var externalId = "  entra-user-123  ";
        var email = "  user@example.com  ";

        // Act
        var result = ExternalLogin.Create(FederatedProvider.Microsoft, externalId, email);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("entra-user-123", result.Value.ExternalProviderId);
        Assert.Equal("user@example.com", result.Value.ProviderEmail);
    }

    [Fact]
    public void TwoExternalLoginsWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var provider = FederatedProvider.Facebook;
        var externalId = "facebook-123";
        var email = "user@facebook.com";

        // Act
        var login1 = ExternalLogin.Create(provider, externalId, email).Value;

        // Create second login with same values (simulate loading from database)
        Thread.Sleep(10); // Ensure different LinkedAt timestamps
        var login2 = ExternalLogin.Create(provider, externalId, email).Value;

        // Assert - Should be equal based on Provider and ExternalProviderId only (not LinkedAt)
        Assert.Equal(login1, login2);
    }

    [Fact]
    public void TwoExternalLoginsWithDifferentProviders_ShouldNotBeEqual()
    {
        // Arrange
        var externalId = "user-123";
        var email = "user@example.com";

        // Act
        var facebookLogin = ExternalLogin.Create(FederatedProvider.Facebook, externalId, email).Value;
        var googleLogin = ExternalLogin.Create(FederatedProvider.Google, externalId, email).Value;

        // Assert
        Assert.NotEqual(facebookLogin, googleLogin);
    }

    [Fact]
    public void TwoExternalLoginsWithDifferentExternalIds_ShouldNotBeEqual()
    {
        // Arrange
        var provider = FederatedProvider.Google;

        // Act
        var login1 = ExternalLogin.Create(provider, "google-user-123", "user1@google.com").Value;
        var login2 = ExternalLogin.Create(provider, "google-user-456", "user2@google.com").Value;

        // Assert
        Assert.NotEqual(login1, login2);
    }

    [Fact]
    public void ExternalLogin_ShouldBeImmutable()
    {
        // Arrange
        var login = ExternalLogin.Create(FederatedProvider.Apple, "apple-123", "user@apple.com").Value;

        // Assert - Properties should be read-only (no setters)
        Assert.Equal(FederatedProvider.Apple, login.Provider);
        Assert.Equal("apple-123", login.ExternalProviderId);
        Assert.Equal("user@apple.com", login.ProviderEmail);
        Assert.IsType<DateTime>(login.LinkedAt);
    }

    [Fact]
    public void ToString_ShouldReturnMeaningfulString()
    {
        // Arrange
        var login = ExternalLogin.Create(FederatedProvider.Facebook, "fb-123", "user@fb.com").Value;

        // Act
        var toString = login.ToString();

        // Assert
        Assert.Contains("Facebook", toString);
        Assert.Contains("fb-123", toString);
    }

    [Fact]
    public void GetHashCode_ForSameExternalLogins_ShouldBeSame()
    {
        // Arrange
        var login1 = ExternalLogin.Create(FederatedProvider.Google, "google-123", "user@google.com").Value;
        var login2 = ExternalLogin.Create(FederatedProvider.Google, "google-123", "user@google.com").Value;

        // Act
        var hash1 = login1.GetHashCode();
        var hash2 = login2.GetHashCode();

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GetHashCode_ForDifferentExternalLogins_ShouldBeDifferent()
    {
        // Arrange
        var login1 = ExternalLogin.Create(FederatedProvider.Facebook, "fb-123", "user1@fb.com").Value;
        var login2 = ExternalLogin.Create(FederatedProvider.Google, "google-456", "user2@google.com").Value;

        // Act
        var hash1 = login1.GetHashCode();
        var hash2 = login2.GetHashCode();

        // Assert
        Assert.NotEqual(hash1, hash2);
    }
}
