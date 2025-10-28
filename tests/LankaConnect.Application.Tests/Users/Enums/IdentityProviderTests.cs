using LankaConnect.Domain.Users.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Enums;

public class IdentityProviderTests
{
    [Fact]
    public void IdentityProvider_ShouldHaveLocalValue()
    {
        // Arrange & Act
        var provider = IdentityProvider.Local;

        // Assert
        Assert.Equal(0, (int)provider);
    }

    [Fact]
    public void IdentityProvider_ShouldHaveEntraExternalValue()
    {
        // Arrange & Act
        var provider = IdentityProvider.EntraExternal;

        // Assert
        Assert.Equal(1, (int)provider);
    }

    [Theory]
    [InlineData(IdentityProvider.Local, "Local")]
    [InlineData(IdentityProvider.EntraExternal, "Microsoft Entra External ID")]
    public void ToDisplayName_ShouldReturnCorrectDisplayName(IdentityProvider provider, string expectedDisplayName)
    {
        // Act
        var displayName = provider.ToDisplayName();

        // Assert
        Assert.Equal(expectedDisplayName, displayName);
    }

    [Theory]
    [InlineData(IdentityProvider.Local, true)]
    [InlineData(IdentityProvider.EntraExternal, false)]
    public void RequiresPasswordHash_ShouldReturnCorrectValue(IdentityProvider provider, bool expectedRequiresPassword)
    {
        // Act
        var requiresPassword = provider.RequiresPasswordHash();

        // Assert
        Assert.Equal(expectedRequiresPassword, requiresPassword);
    }

    [Theory]
    [InlineData(IdentityProvider.Local, false)]
    [InlineData(IdentityProvider.EntraExternal, true)]
    public void RequiresExternalProviderId_ShouldReturnCorrectValue(IdentityProvider provider, bool expectedRequiresExternalId)
    {
        // Act
        var requiresExternalId = provider.RequiresExternalProviderId();

        // Assert
        Assert.Equal(expectedRequiresExternalId, requiresExternalId);
    }

    [Theory]
    [InlineData(IdentityProvider.Local, false)]
    [InlineData(IdentityProvider.EntraExternal, true)]
    public void IsExternalProvider_ShouldReturnCorrectValue(IdentityProvider provider, bool expectedIsExternal)
    {
        // Act
        var isExternal = provider.IsExternalProvider();

        // Assert
        Assert.Equal(expectedIsExternal, isExternal);
    }

    [Theory]
    [InlineData(IdentityProvider.Local, false)]
    [InlineData(IdentityProvider.EntraExternal, true)]
    public void EmailPreVerified_ShouldReturnCorrectValue(IdentityProvider provider, bool expectedEmailPreVerified)
    {
        // Act
        var emailPreVerified = provider.EmailPreVerified();

        // Assert
        Assert.Equal(expectedEmailPreVerified, emailPreVerified);
    }
}
