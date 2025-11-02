using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// Tests for User aggregate external login management
/// Following TDD: Write tests first (RED phase)
/// </summary>
public class UserExternalLoginsTests
{
    private static Result<User> CreateTestUser()
    {
        var email = Email.Create("test@example.com");
        return User.Create(email.Value, "John", "Doe");
    }

    [Fact]
    public void NewUser_ShouldHaveEmptyExternalLogins()
    {
        // Arrange & Act
        var user = CreateTestUser().Value;

        // Assert
        Assert.NotNull(user.ExternalLogins);
        Assert.Empty(user.ExternalLogins);
    }

    [Fact]
    public void LinkExternalProvider_WithValidData_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var provider = FederatedProvider.Facebook;
        var externalId = "facebook-user-123";
        var providerEmail = "user@facebook.com";

        // Act
        var result = user.LinkExternalProvider(provider, externalId, providerEmail);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(user.ExternalLogins);
        Assert.Contains(user.ExternalLogins, login =>
            login.Provider == provider &&
            login.ExternalProviderId == externalId);
    }

    [Fact]
    public void LinkExternalProvider_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.ClearDomainEvents(); // Clear UserCreatedEvent

        // Act
        var result = user.LinkExternalProvider(FederatedProvider.Google, "google-123", "user@google.com");

        // Assert
        Assert.True(result.IsSuccess);
        var domainEvent = user.DomainEvents.OfType<ExternalProviderLinkedEvent>().FirstOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(user.Id, domainEvent.UserId);
        Assert.Equal(FederatedProvider.Google, domainEvent.Provider);
        Assert.Equal("google-123", domainEvent.ExternalProviderId);
    }

    [Fact]
    public void LinkExternalProvider_WhenAlreadyLinked_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var provider = FederatedProvider.Apple;
        var externalId = "apple-user-123";
        user.LinkExternalProvider(provider, externalId, "user@apple.com");

        // Act - Try to link same provider again
        var result = user.LinkExternalProvider(provider, externalId, "user@apple.com");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("already linked", result.Error);
        Assert.Single(user.ExternalLogins); // Should still have only one
    }

    [Fact]
    public void LinkExternalProvider_MultipleProviders_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUser().Value;

        // Act
        user.LinkExternalProvider(FederatedProvider.Facebook, "fb-123", "user@fb.com");
        user.LinkExternalProvider(FederatedProvider.Google, "google-456", "user@google.com");
        user.LinkExternalProvider(FederatedProvider.Apple, "apple-789", "user@apple.com");

        // Assert
        Assert.Equal(3, user.ExternalLogins.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LinkExternalProvider_WithInvalidExternalId_ShouldReturnFailure(string invalidId)
    {
        // Arrange
        var user = CreateTestUser().Value;

        // Act
        var result = user.LinkExternalProvider(FederatedProvider.Facebook, invalidId, "user@fb.com");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("External provider ID", result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void LinkExternalProvider_WithInvalidProviderEmail_ShouldReturnFailure(string invalidEmail)
    {
        // Arrange
        var user = CreateTestUser().Value;

        // Act
        var result = user.LinkExternalProvider(FederatedProvider.Google, "google-123", invalidEmail);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Provider email", result.Error);
    }

    [Fact]
    public void UnlinkExternalProvider_WithExistingProvider_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.SetPassword("hashed-password"); // Need password to allow unlinking
        var provider = FederatedProvider.Facebook;
        user.LinkExternalProvider(provider, "fb-123", "user@fb.com");

        // Act
        var result = user.UnlinkExternalProvider(provider);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(user.ExternalLogins);
    }

    [Fact]
    public void UnlinkExternalProvider_ShouldRaiseDomainEvent()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.SetPassword("hashed-password"); // Need password to allow unlinking
        user.LinkExternalProvider(FederatedProvider.Google, "google-123", "user@google.com");
        user.ClearDomainEvents(); // Clear previous events

        // Act
        var result = user.UnlinkExternalProvider(FederatedProvider.Google);

        // Assert
        Assert.True(result.IsSuccess);
        var domainEvent = user.DomainEvents.OfType<ExternalProviderUnlinkedEvent>().FirstOrDefault();
        Assert.NotNull(domainEvent);
        Assert.Equal(user.Id, domainEvent.UserId);
        Assert.Equal(FederatedProvider.Google, domainEvent.Provider);
    }

    [Fact]
    public void UnlinkExternalProvider_WhenNotLinked_ShouldReturnFailure()
    {
        // Arrange
        var user = CreateTestUser().Value;

        // Act
        var result = user.UnlinkExternalProvider(FederatedProvider.Facebook);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not linked", result.Error);
    }

    [Fact]
    public void UnlinkExternalProvider_WhenLastAuthMethod_ShouldReturnFailure()
    {
        // Arrange - User with ONLY external login (no password)
        var email = Email.Create("external@example.com").Value;
        var user = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            "entra-123",
            email,
            "John",
            "Doe").Value;

        // Link a single Facebook provider
        user.LinkExternalProvider(FederatedProvider.Facebook, "fb-123", "user@fb.com");

        // Act - Try to unlink the only authentication method
        var result = user.UnlinkExternalProvider(FederatedProvider.Facebook);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("last authentication method", result.Error);
    }

    [Fact]
    public void UnlinkExternalProvider_WhenUserHasPassword_ShouldSucceed()
    {
        // Arrange - User with password + external login
        var user = CreateTestUser().Value;
        user.SetPassword("hashed-password-123");
        user.LinkExternalProvider(FederatedProvider.Google, "google-123", "user@google.com");

        // Act - Unlink external provider (still has password)
        var result = user.UnlinkExternalProvider(FederatedProvider.Google);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(user.ExternalLogins);
    }

    [Fact]
    public void UnlinkExternalProvider_WhenUserHasOtherProviders_ShouldSucceed()
    {
        // Arrange - User with multiple external logins
        var email = Email.Create("external@example.com").Value;
        var user = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            "entra-123",
            email,
            "John",
            "Doe").Value;

        user.LinkExternalProvider(FederatedProvider.Facebook, "fb-123", "user@fb.com");
        user.LinkExternalProvider(FederatedProvider.Google, "google-456", "user@google.com");

        // Act - Unlink one provider (still has another)
        var result = user.UnlinkExternalProvider(FederatedProvider.Facebook);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(user.ExternalLogins);
        Assert.Contains(user.ExternalLogins, login => login.Provider == FederatedProvider.Google);
    }

    [Fact]
    public void HasExternalLogin_WhenLinked_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser().Value;
        user.LinkExternalProvider(FederatedProvider.Apple, "apple-123", "user@apple.com");

        // Act
        var hasApple = user.HasExternalLogin(FederatedProvider.Apple);
        var hasFacebook = user.HasExternalLogin(FederatedProvider.Facebook);

        // Assert
        Assert.True(hasApple);
        Assert.False(hasFacebook);
    }

    [Fact]
    public void GetExternalLogin_WhenLinked_ShouldReturnLogin()
    {
        // Arrange
        var user = CreateTestUser().Value;
        var externalId = "google-123";
        user.LinkExternalProvider(FederatedProvider.Google, externalId, "user@google.com");

        // Act
        var login = user.GetExternalLogin(FederatedProvider.Google);

        // Assert
        Assert.NotNull(login);
        Assert.Equal(FederatedProvider.Google, login.Provider);
        Assert.Equal(externalId, login.ExternalProviderId);
    }

    [Fact]
    public void GetExternalLogin_WhenNotLinked_ShouldReturnNull()
    {
        // Arrange
        var user = CreateTestUser().Value;

        // Act
        var login = user.GetExternalLogin(FederatedProvider.Facebook);

        // Assert
        Assert.Null(login);
    }
}
