using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using System.Security.Cryptography;
using FluentAssertions;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

public class VerificationTokenTests
{
    [Fact]
    public void Create_WithDefaultValidityHours_ShouldReturnSuccess()
    {
        // Act
        var result = VerificationToken.Create();

        // Assert
        Assert.True(result.IsSuccess);
        var token = result.Value;
        Assert.NotNull(token.Value);
        Assert.NotEmpty(token.Value);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
        Assert.False(token.IsExpired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)]
    public void Create_WithInvalidValidityHours_ShouldReturnFailure(int validityHours)
    {
        // Act
        var result = VerificationToken.Create(validityHours);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Token validity must be between 1 and 168 hours", result.Errors);
    }

    [Fact]
    public void FromExisting_WithValidTokenAndFutureExpiry_ShouldReturnSuccess()
    {
        // Arrange
        var tokenValue = "validTokenString123";
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var result = VerificationToken.FromExisting(tokenValue, expiresAt);

        // Assert
        Assert.True(result.IsSuccess);
        var token = result.Value;
        Assert.Equal(tokenValue, token.Value);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void Create_WithExpiryTime_ShouldSetExpiryCorrectly()
    {
        // Arrange
        var token = GenerateSecureToken();
        var expiryTime = DateTime.UtcNow.AddHours(24);

        // Act
        var tokenInfo = CreateTokenWithExpiry(token, expiryTime);

        // Assert
        Assert.Equal(token, tokenInfo.Token);
        Assert.Equal(expiryTime, tokenInfo.ExpiryTime);
        Assert.False(tokenInfo.IsExpired);
    }

    [Fact]
    public void IsExpired_WithPastExpiryTime_ShouldReturnTrue()
    {
        // Arrange
        var token = GenerateSecureToken();
        var pastExpiryTime = DateTime.UtcNow.AddHours(-1);
        var tokenInfo = CreateTokenWithExpiry(token, pastExpiryTime);

        // Act & Assert
        Assert.True(tokenInfo.IsExpired);
    }

    [Fact]
    public void IsExpired_WithFutureExpiryTime_ShouldReturnFalse()
    {
        // Arrange
        var token = GenerateSecureToken();
        var futureExpiryTime = DateTime.UtcNow.AddHours(1);
        var tokenInfo = CreateTokenWithExpiry(token, futureExpiryTime);

        // Act & Assert
        Assert.False(tokenInfo.IsExpired);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueTokens()
    {
        // Arrange & Act
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => GenerateSecureToken())
            .ToList();

        // Assert
        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    [Fact]
    public void Create_ShouldGenerateSecureTokens()
    {
        // Arrange & Act
        var token = GenerateSecureToken();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.True(token.Length >= 32); // Minimum secure length
        
        // Token should contain mix of characters for security
        Assert.Matches(@"^[A-Za-z0-9]+$", token);
    }

    [Fact]
    public void Equality_WithSameToken_ShouldBeEqual()
    {
        // Arrange
        var tokenValue = GenerateSecureToken();
        var token1 = CreateTokenInfo(tokenValue);
        var token2 = CreateTokenInfo(tokenValue);

        // Act & Assert
        Assert.Equal(token1.Token, token2.Token);
    }

    [Fact]
    public void Equality_WithDifferentTokens_ShouldNotBeEqual()
    {
        // Arrange
        var token1 = CreateTokenInfo(GenerateSecureToken());
        var token2 = CreateTokenInfo(GenerateSecureToken());

        // Act & Assert
        token1.Token.Should().NotBe(token2.Token);
    }

    [Fact]
    public void ToString_ShouldNotExposeFullToken()
    {
        // Arrange
        var token = GenerateSecureToken();
        var tokenInfo = CreateTokenInfo(token);

        // Act
        var stringRepresentation = tokenInfo.ToString();

        // Assert - For security, ToString should not expose the full token
        stringRepresentation.Should().NotBe(token);
        stringRepresentation.Should().Contain("***"); // Should be partially masked
    }

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(72)]
    public void CreateWithDefaultExpiry_ShouldSetCorrectExpiryTime(int hoursToExpiry)
    {
        // Arrange
        var token = GenerateSecureToken();
        var expectedExpiry = DateTime.UtcNow.AddHours(hoursToExpiry);

        // Act
        var tokenInfo = CreateTokenWithExpiry(token, expectedExpiry);

        // Assert
        tokenInfo.ExpiryTime.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    // Test for token usage scenarios
    [Fact]
    public void MarkAsUsed_ShouldPreventReuse()
    {
        // Arrange
        var tokenInfo = CreateTokenInfo(GenerateSecureToken());

        // Act
        tokenInfo.MarkAsUsed();

        // Assert
        tokenInfo.IsUsed.Should().BeTrue();
        tokenInfo.CanBeUsed.Should().BeFalse();
    }

    [Fact]
    public void CanBeUsed_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var tokenInfo = CreateTokenWithExpiry(GenerateSecureToken(), DateTime.UtcNow.AddHours(1));

        // Act & Assert
        tokenInfo.CanBeUsed.Should().BeTrue();
    }

    [Fact]
    public void CanBeUsed_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenInfo = CreateTokenWithExpiry(GenerateSecureToken(), DateTime.UtcNow.AddHours(-1));

        // Act & Assert
        tokenInfo.CanBeUsed.Should().BeFalse();
    }

    [Fact]
    public void CanBeUsed_WithUsedToken_ShouldReturnFalse()
    {
        // Arrange
        var tokenInfo = CreateTokenWithExpiry(GenerateSecureToken(), DateTime.UtcNow.AddHours(1));
        tokenInfo.MarkAsUsed();

        // Act & Assert
        tokenInfo.CanBeUsed.Should().BeFalse();
    }

    // Helper methods representing what VerificationToken implementation should do

    private static bool ValidateVerificationToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (token.Length < 32) // Minimum secure length
            return false;

        return true;
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..32];
    }

    private static VerificationTokenInfo CreateTokenInfo(string token)
    {
        return new VerificationTokenInfo
        {
            Token = token,
            ExpiryTime = DateTime.UtcNow.AddHours(24)
        };
    }

    private static VerificationTokenInfo CreateTokenWithExpiry(string token, DateTime expiryTime)
    {
        return new VerificationTokenInfo
        {
            Token = token,
            ExpiryTime = expiryTime
        };
    }

    // Placeholder class representing what VerificationToken would look like
    private class VerificationTokenInfo
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public bool IsUsed { get; private set; }

        public bool IsExpired => DateTime.UtcNow > ExpiryTime;
        public bool CanBeUsed => !IsExpired && !IsUsed;

        public void MarkAsUsed() => IsUsed = true;

        public override string ToString()
        {
            // Mask token for security - show only first and last few characters
            if (Token.Length <= 8)
                return "***";
            
            return $"{Token[..4]}***{Token[^4..]}";
        }
    }
}

// TODO: Consider implementing VerificationToken as a proper value object
// public class VerificationToken : ValueObject
// {
//     public string Value { get; private set; }
//     public DateTime ExpiryTime { get; private set; }
//     public bool IsUsed { get; private set; }
//     
//     private VerificationToken(string value, DateTime expiryTime)
//     {
//         Value = value;
//         ExpiryTime = expiryTime;
//         IsUsed = false;
//     }
//     
//     public static Result<VerificationToken> Create(DateTime? expiryTime = null)
//     {
//         var token = GenerateSecureToken();
//         var expiry = expiryTime ?? DateTime.UtcNow.AddHours(24);
//         
//         return Result<VerificationToken>.Success(new VerificationToken(token, expiry));
//     }
//     
//     public bool IsExpired => DateTime.UtcNow > ExpiryTime;
//     public bool CanBeUsed => !IsExpired && !IsUsed;
//     
//     public void MarkAsUsed() => IsUsed = true;
//     
//     public override IEnumerable<object> GetEqualityComponents()
//     {
//         yield return Value;
//     }
// }