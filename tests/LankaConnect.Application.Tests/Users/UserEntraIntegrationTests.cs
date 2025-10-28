using Xunit;
using FluentAssertions;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Common;
using Email = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Application.Tests.Users;

/// <summary>
/// Tests for Microsoft Entra External ID integration with User entity
/// Following ADR-002: Entra External ID Integration
/// </summary>
public class UserEntraIntegrationTests
{
    #region IdentityProvider Property Tests

    [Fact]
    public void Create_LocalUser_ShouldDefaultToLocalProvider()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var userResult = User.Create(email, "John", "Doe");

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.IdentityProvider.Should().Be(IdentityProvider.Local);
    }

    [Fact]
    public void Create_LocalUser_ShouldNotHaveExternalProviderId()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var userResult = User.Create(email, "John", "Doe");

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.ExternalProviderId.Should().BeNull();
    }

    #endregion

    #region CreateFromExternalProvider Factory Method Tests

    [Fact]
    public void CreateFromExternalProvider_WithValidData_ShouldSucceed()
    {
        // Arrange
        var email = Email.Create("johndoe@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef"; // Entra OID claim
        var firstName = "John";
        var lastName = "Doe";

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            firstName,
            lastName);

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
        userResult.Value.ExternalProviderId.Should().Be(externalProviderId);
        userResult.Value.Email.Value.Should().Be("johndoe@example.com");
        userResult.Value.FirstName.Should().Be(firstName);
        userResult.Value.LastName.Should().Be(lastName);
        userResult.Value.IsEmailVerified.Should().BeTrue(); // Entra pre-verifies emails
        userResult.Value.PasswordHash.Should().BeNull(); // External providers don't need password
    }

    [Fact]
    public void CreateFromExternalProvider_WithNullEmail_ShouldFail()
    {
        // Arrange
        var externalProviderId = "oid:1234567890abcdef";

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            null,
            "John",
            "Doe");

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Errors.Should().Contain("Email is required");
    }

    [Fact]
    public void CreateFromExternalProvider_WithNullExternalProviderId_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            null,
            email,
            "John",
            "Doe");

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Errors.Should().Contain("External provider ID is required for external providers");
    }

    [Fact]
    public void CreateFromExternalProvider_WithEmptyExternalProviderId_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            "   ",
            email,
            "John",
            "Doe");

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Errors.Should().Contain("External provider ID is required for external providers");
    }

    [Fact]
    public void CreateFromExternalProvider_WithLocalProvider_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.Local,
            "some-id",
            email,
            "John",
            "Doe");

        // Assert
        userResult.IsFailure.Should().BeTrue();
        userResult.Errors.Should().Contain("Cannot create user from external provider using Local identity provider");
    }

    [Fact]
    public void CreateFromExternalProvider_ShouldSetEmailAsVerified()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public void CreateFromExternalProvider_ShouldNotRequirePasswordHash()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        userResult.Value.PasswordHash.Should().BeNull();
    }

    #endregion

    #region Business Rules Validation Tests

    [Fact]
    public void SetPassword_OnExternalProviderUser_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");
        var user = userResult.Value;

        // Act
        var setPasswordResult = user.SetPassword("hashedPassword123");

        // Assert
        setPasswordResult.IsFailure.Should().BeTrue();
        setPasswordResult.Errors.Should().Contain("Cannot set password for external provider users");
    }

    [Fact]
    public void SetPassword_OnLocalProviderUser_ShouldSucceed()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var userResult = User.Create(email, "John", "Doe");
        var user = userResult.Value;

        // Act
        var setPasswordResult = user.SetPassword("hashedPassword123");

        // Assert
        setPasswordResult.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("hashedPassword123");
    }

    [Fact]
    public void ChangePassword_OnExternalProviderUser_ShouldFail()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");
        var user = userResult.Value;

        // Act
        var changePasswordResult = user.ChangePassword("newHashedPassword123");

        // Assert
        changePasswordResult.IsFailure.Should().BeTrue();
        changePasswordResult.Errors.Should().Contain("Cannot change password for external provider users");
    }

    [Fact]
    public void IsLocalProvider_ShouldReturnTrueForLocalUsers()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var userResult = User.Create(email, "John", "Doe");
        var user = userResult.Value;

        // Act
        var isLocal = user.IsLocalProvider();

        // Assert
        isLocal.Should().BeTrue();
    }

    [Fact]
    public void IsLocalProvider_ShouldReturnFalseForExternalUsers()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");
        var user = userResult.Value;

        // Act
        var isLocal = user.IsLocalProvider();

        // Assert
        isLocal.Should().BeFalse();
    }

    [Fact]
    public void IsExternalProvider_ShouldReturnTrueForExternalUsers()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");
        var user = userResult.Value;

        // Act
        var isExternal = user.IsExternalProvider();

        // Assert
        isExternal.Should().BeTrue();
    }

    #endregion

    #region Domain Events Tests

    [Fact]
    public void CreateFromExternalProvider_ShouldRaiseUserCreatedFromExternalProviderEvent()
    {
        // Arrange
        var email = Email.Create("test@example.com").Value;
        var externalProviderId = "oid:1234567890abcdef";

        // Act
        var userResult = User.CreateFromExternalProvider(
            IdentityProvider.EntraExternal,
            externalProviderId,
            email,
            "John",
            "Doe");

        // Assert
        userResult.IsSuccess.Should().BeTrue();
        var user = userResult.Value;
        user.DomainEvents.Should().ContainSingle();
        user.DomainEvents.First().GetType().Name.Should().Be("UserCreatedFromExternalProviderEvent");
    }

    #endregion
}
