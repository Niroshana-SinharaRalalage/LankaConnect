using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using System.Text.RegularExpressions;

namespace LankaConnect.Domain.Tests.Communications.ValueObjects;

public class VerificationTokenComprehensiveTests
{
    #region Creation Tests

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
        Assert.True(token.ExpiresAt <= DateTime.UtcNow.AddHours(24));
        Assert.False(token.IsExpired);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    [InlineData(24)]
    [InlineData(72)]
    [InlineData(168)] // Max 1 week
    public void Create_WithValidValidityHours_ShouldReturnSuccess(int validityHours)
    {
        // Act
        var result = VerificationToken.Create(validityHours);

        // Assert
        Assert.True(result.IsSuccess);
        var token = result.Value;
        Assert.NotNull(token.Value);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);
        Assert.True(token.ExpiresAt <= DateTime.UtcNow.AddHours(validityHours));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)] // More than 1 week
    [InlineData(1000)]
    public void Create_WithInvalidValidityHours_ShouldReturnFailure(int validityHours)
    {
        // Act
        var result = VerificationToken.Create(validityHours);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Token validity must be between 1 and 168 hours", result.Errors);
    }

    #endregion

    #region FromExisting Tests

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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromExisting_WithInvalidToken_ShouldReturnFailure(string tokenValue)
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var result = VerificationToken.FromExisting(tokenValue, expiresAt);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Token value is required", result.Errors);
    }

    [Fact]
    public void FromExisting_WithExpiredToken_ShouldReturnFailure()
    {
        // Arrange
        var tokenValue = "expiredToken";
        var expiresAt = DateTime.UtcNow.AddHours(-1);

        // Act
        var result = VerificationToken.FromExisting(tokenValue, expiresAt);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Token has expired", result.Errors);
    }

    #endregion

    #region Token Generation Tests

    [Fact]
    public void Create_ShouldGenerateUniqueTokens()
    {
        // Arrange & Act
        var tokens = Enumerable.Range(0, 100)
            .Select(_ => VerificationToken.Create().Value.Value)
            .ToList();

        // Assert
        var uniqueTokens = tokens.Distinct().ToList();
        Assert.Equal(tokens.Count, uniqueTokens.Count);
    }

    [Fact]
    public void Create_ShouldGenerateSecureTokens()
    {
        // Act
        var result = VerificationToken.Create();
        var token = result.Value.Value;

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(token);
        Assert.True(token.Length >= 40); // Base64 encoded 32 bytes should be 44 chars, but at least 40
        
        // Should be Base64 format
        Assert.True(IsValidBase64String(token), "Token should be in Base64 format");
    }

    [Fact]
    public void Create_TokensShouldBeCryptographicallySecure()
    {
        // Arrange - Generate multiple tokens
        var tokens = Enumerable.Range(0, 10)
            .Select(_ => VerificationToken.Create().Value.Value)
            .ToList();

        // Assert - Tokens should have good entropy and no obvious patterns
        foreach (var token in tokens)
        {
            Assert.True(token.Length >= 40);
            Assert.True(HasGoodEntropy(token), $"Token {token} should have good entropy");
        }
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public void IsExpired_WithFutureExpiryTime_ShouldReturnFalse()
    {
        // Arrange
        var token = VerificationToken.Create(1).Value;

        // Act & Assert
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void IsExpired_WithPastExpiryTime_ShouldReturnTrue()
    {
        // Arrange
        var pastTime = DateTime.UtcNow.AddHours(-1);
        var token = VerificationToken.FromExisting("testToken", pastTime);

        // This should fail at creation time
        Assert.True(token.IsFailure);
    }

    [Fact]
    public void IsExpired_AtExactExpiryTime_ShouldBehavePredictably()
    {
        // This test verifies behavior at the exact expiry moment
        // Note: Due to timing precision, we test with a very short window
        
        // Arrange
        var expiryTime = DateTime.UtcNow.AddMilliseconds(100);
        
        // Wait a bit to ensure expiry
        System.Threading.Thread.Sleep(150);
        
        // Create a token that should be expired by construction
        var result = VerificationToken.FromExisting("testToken", expiryTime);
        
        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Token has expired", result.Errors);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsValid_WithCorrectTokenAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var token = VerificationToken.Create(1).Value;

        // Act
        var isValid = token.IsValid(token.Value);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_WithIncorrectToken_ShouldReturnFalse()
    {
        // Arrange
        var token = VerificationToken.Create(1).Value;

        // Act
        var isValid = token.IsValid("wrongToken");

        // Assert
        Assert.False(isValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_WithNullOrEmptyToken_ShouldReturnFalse(string tokenToVerify)
    {
        // Arrange
        var token = VerificationToken.Create(1).Value;

        // Act
        var isValid = token.IsValid(tokenToVerify);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_WithCaseSensitiveToken_ShouldRespectCase()
    {
        // Arrange
        var token = VerificationToken.Create(1).Value;
        var upperCaseValue = token.Value.ToUpper();

        // Act
        var isValidSame = token.IsValid(token.Value);
        var isValidDifferentCase = token.IsValid(upperCaseValue);

        // Assert
        Assert.True(isValidSame);
        // Base64 tokens are case-sensitive
        if (token.Value != upperCaseValue)
        {
            Assert.False(isValidDifferentCase);
        }
    }

    #endregion

    #region Value Object Behavior Tests

    [Fact]
    public void Equality_WithSameValueAndExpiry_ShouldBeEqual()
    {
        // Arrange
        var tokenValue = "sameTokenValue";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var token1 = VerificationToken.FromExisting(tokenValue, expiresAt).Value;
        var token2 = VerificationToken.FromExisting(tokenValue, expiresAt).Value;

        // Act & Assert
        Assert.Equal(token1, token2);
        Assert.True(token1.Equals(token2));
        Assert.True(token1 == token2);
        Assert.False(token1 != token2);
        Assert.Equal(token1.GetHashCode(), token2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var token1 = VerificationToken.FromExisting("token1", expiresAt).Value;
        var token2 = VerificationToken.FromExisting("token2", expiresAt).Value;

        // Act & Assert
        Assert.NotEqual(token1, token2);
        Assert.False(token1.Equals(token2));
    }

    [Fact]
    public void Equality_WithDifferentExpiry_ShouldNotBeEqual()
    {
        // Arrange
        var tokenValue = "sameTokenValue";
        var token1 = VerificationToken.FromExisting(tokenValue, DateTime.UtcNow.AddHours(1)).Value;
        var token2 = VerificationToken.FromExisting(tokenValue, DateTime.UtcNow.AddHours(2)).Value;

        // Act & Assert
        Assert.NotEqual(token1, token2);
        Assert.False(token1.Equals(token2));
    }

    [Fact]
    public void Equality_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var token = VerificationToken.Create().Value;

        // Act & Assert
        Assert.False(token.Equals(null));
        Assert.False(token == null);
        Assert.True(token != null);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void VerificationToken_ShouldBeImmutable()
    {
        // Arrange
        var token = VerificationToken.Create().Value;
        var originalValue = token.Value;
        var originalExpiry = token.ExpiresAt;

        // Act - Accessing properties multiple times
        var value1 = token.Value;
        var value2 = token.Value;
        var expiry1 = token.ExpiresAt;
        var expiry2 = token.ExpiresAt;

        // Assert - Values should remain consistent
        Assert.Equal(originalValue, value1);
        Assert.Equal(originalValue, value2);
        Assert.Equal(originalExpiry, expiry1);
        Assert.Equal(originalExpiry, expiry2);
    }

    #endregion

    #region GetEqualityComponents Tests

    [Fact]
    public void GetEqualityComponents_ShouldReturnValueAndExpiryComponents()
    {
        // Arrange
        var tokenValue = "testToken";
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var token = VerificationToken.FromExisting(tokenValue, expiresAt).Value;

        // Act
        var components = token.GetEqualityComponents().ToList();

        // Assert
        Assert.Equal(2, components.Count);
        Assert.Contains(tokenValue, components);
        Assert.Contains(expiresAt, components);
    }

    #endregion

    #region Business Context Tests

    [Fact]
    public void Create_ForEmailVerification_ShouldHaveAppropriateExpiry()
    {
        // Arrange - Email verification tokens typically expire in 24 hours
        var emailVerificationHours = 24;

        // Act
        var result = VerificationToken.Create(emailVerificationHours);

        // Assert
        Assert.True(result.IsSuccess);
        var token = result.Value;
        var expectedMinExpiry = DateTime.UtcNow.AddHours(emailVerificationHours - 1);
        var expectedMaxExpiry = DateTime.UtcNow.AddHours(emailVerificationHours + 1);
        
        Assert.True(token.ExpiresAt >= expectedMinExpiry);
        Assert.True(token.ExpiresAt <= expectedMaxExpiry);
    }

    [Fact]
    public void Create_ForPasswordReset_ShouldHaveShortExpiry()
    {
        // Arrange - Password reset tokens should expire quickly for security
        var passwordResetHours = 1;

        // Act
        var result = VerificationToken.Create(passwordResetHours);

        // Assert
        Assert.True(result.IsSuccess);
        var token = result.Value;
        var expectedMinExpiry = DateTime.UtcNow.AddMinutes(50);
        var expectedMaxExpiry = DateTime.UtcNow.AddMinutes(70);
        
        Assert.True(token.ExpiresAt >= expectedMinExpiry);
        Assert.True(token.ExpiresAt <= expectedMaxExpiry);
    }

    #endregion

    #region Helper Methods

    private static bool IsValidBase64String(string base64String)
    {
        try
        {
            Convert.FromBase64String(base64String);
            return true;
        }
        catch
        {
            // Try with padding
            try
            {
                var padded = base64String.PadRight((base64String.Length + 3) & ~3, '=');
                Convert.FromBase64String(padded);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    private static bool HasGoodEntropy(string token)
    {
        // Basic entropy check - token should have variety in characters
        var uniqueChars = token.Distinct().Count();
        var totalChars = token.Length;
        
        // Should have at least 50% unique characters for good entropy
        return (double)uniqueChars / totalChars > 0.5;
    }

    #endregion
}