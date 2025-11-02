using LankaConnect.Domain.Users.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Users.Domain;

/// <summary>
/// Tests for FederatedProvider enum and its extension methods
/// Following TDD: Write tests first (RED phase)
/// </summary>
public class FederatedProviderTests
{
    [Fact]
    public void FederatedProvider_ShouldHave_FourProviders()
    {
        // Arrange & Act
        var providers = Enum.GetValues<FederatedProvider>();

        // Assert
        Assert.Equal(4, providers.Length);
        Assert.Contains(FederatedProvider.Microsoft, providers);
        Assert.Contains(FederatedProvider.Facebook, providers);
        Assert.Contains(FederatedProvider.Google, providers);
        Assert.Contains(FederatedProvider.Apple, providers);
    }

    [Theory]
    [InlineData(FederatedProvider.Microsoft, "Microsoft")]
    [InlineData(FederatedProvider.Facebook, "Facebook")]
    [InlineData(FederatedProvider.Google, "Google")]
    [InlineData(FederatedProvider.Apple, "Apple")]
    public void ToDisplayName_ShouldReturnCorrectName(FederatedProvider provider, string expectedName)
    {
        // Act
        var displayName = provider.ToDisplayName();

        // Assert
        Assert.Equal(expectedName, displayName);
    }

    [Theory]
    [InlineData("login.microsoftonline.com", FederatedProvider.Microsoft)]
    [InlineData("facebook.com", FederatedProvider.Facebook)]
    [InlineData("google.com", FederatedProvider.Google)]
    [InlineData("appleid.apple.com", FederatedProvider.Apple)]
    public void FromIdpClaimValue_WithValidClaim_ShouldReturnCorrectProvider(
        string idpClaim, FederatedProvider expectedProvider)
    {
        // Act
        var result = FederatedProviderExtensions.FromIdpClaimValue(idpClaim);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedProvider, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid.provider.com")]
    public void FromIdpClaimValue_WithInvalidClaim_ShouldReturnFailure(string invalidClaim)
    {
        // Act
        var result = FederatedProviderExtensions.FromIdpClaimValue(invalidClaim);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("identity provider claim", result.Error);
    }

    [Theory]
    [InlineData(FederatedProvider.Microsoft, "login.microsoftonline.com")]
    [InlineData(FederatedProvider.Facebook, "facebook.com")]
    [InlineData(FederatedProvider.Google, "google.com")]
    [InlineData(FederatedProvider.Apple, "appleid.apple.com")]
    public void ToIdpClaimValue_ShouldReturnCorrectClaimValue(
        FederatedProvider provider, string expectedClaim)
    {
        // Act
        var claimValue = provider.ToIdpClaimValue();

        // Assert
        Assert.Equal(expectedClaim, claimValue);
    }

    [Fact]
    public void FromIdpClaimValue_ShouldBeCaseInsensitive()
    {
        // Act
        var result1 = FederatedProviderExtensions.FromIdpClaimValue("FACEBOOK.COM");
        var result2 = FederatedProviderExtensions.FromIdpClaimValue("Facebook.COM");
        var result3 = FederatedProviderExtensions.FromIdpClaimValue("facebook.com");

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.True(result3.IsSuccess);
        Assert.Equal(FederatedProvider.Facebook, result1.Value);
        Assert.Equal(FederatedProvider.Facebook, result2.Value);
        Assert.Equal(FederatedProvider.Facebook, result3.Value);
    }

    [Fact]
    public void Microsoft_ShouldBeDefaultProvider()
    {
        // Arrange & Act
        var microsoft = FederatedProvider.Microsoft;

        // Assert
        Assert.Equal(0, (int)microsoft);
    }
}
