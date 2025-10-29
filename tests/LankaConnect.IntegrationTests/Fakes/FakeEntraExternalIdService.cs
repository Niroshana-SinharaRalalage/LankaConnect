using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.IntegrationTests.Fakes;

/// <summary>
/// Fake implementation of IEntraExternalIdService for deterministic integration testing
/// Returns predictable responses based on test token constants
/// </summary>
public class FakeEntraExternalIdService : IEntraExternalIdService
{
    private readonly Dictionary<string, EntraUserInfo> _mockUsers;
    private readonly HashSet<string> _expiredTokens;
    private readonly HashSet<string> _invalidTokens;

    public bool IsEnabled { get; set; } = true;

    public FakeEntraExternalIdService()
    {
        _mockUsers = new Dictionary<string, EntraUserInfo>();
        _expiredTokens = new HashSet<string>();
        _invalidTokens = new HashSet<string>();

        // Initialize test users for common scenarios
        InitializeTestUsers();
    }

    private void InitializeTestUsers()
    {
        // Scenario 1: New user auto-provisioning
        _mockUsers[TestEntraTokens.ValidTokenNewUser] = new EntraUserInfo
        {
            ObjectId = "entra-oid-new-user-12345",
            Email = "newuser@example.com",
            FirstName = "New",
            LastName = "User",
            DisplayName = "New User",
            EmailVerified = true
        };

        // Scenario 2: Existing user login
        _mockUsers[TestEntraTokens.ValidTokenExistingUser] = new EntraUserInfo
        {
            ObjectId = "entra-oid-existing-user-67890",
            Email = "existinguser@example.com",
            FirstName = "Existing",
            LastName = "User",
            DisplayName = "Existing User",
            EmailVerified = true
        };

        // Scenario 3: User with profile updates
        _mockUsers[TestEntraTokens.ValidTokenProfileUpdate] = new EntraUserInfo
        {
            ObjectId = "entra-oid-profile-update-11111",
            Email = "profileupdate@example.com",
            FirstName = "Updated",
            LastName = "Name",
            DisplayName = "Updated Name",
            EmailVerified = true
        };

        // Scenario 4: Duplicate email conflict (local user exists)
        _mockUsers[TestEntraTokens.ValidTokenDuplicateEmail] = new EntraUserInfo
        {
            ObjectId = "entra-oid-duplicate-22222",
            Email = "duplicate@example.com",
            FirstName = "Duplicate",
            LastName = "User",
            DisplayName = "Duplicate User",
            EmailVerified = true
        };

        // Expired tokens
        _expiredTokens.Add(TestEntraTokens.ExpiredToken);

        // Invalid tokens
        _invalidTokens.Add(TestEntraTokens.InvalidToken);
        _invalidTokens.Add("invalid-malformed-token-12345");
    }

    public Task<Result<EntraTokenClaims>> ValidateAccessTokenAsync(string accessToken)
    {
        // Check if service is disabled
        if (!IsEnabled)
        {
            return Task.FromResult(Result<EntraTokenClaims>.Failure(
                "Entra External ID authentication is not enabled"));
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(Result<EntraTokenClaims>.Failure(
                "Access token is required"));
        }

        // Check for expired tokens
        if (_expiredTokens.Contains(accessToken))
        {
            return Task.FromResult(Result<EntraTokenClaims>.Failure(
                "Token has expired"));
        }

        // Check for invalid tokens
        if (_invalidTokens.Contains(accessToken))
        {
            return Task.FromResult(Result<EntraTokenClaims>.Failure(
                "Invalid access token"));
        }

        // Check for known test users
        if (_mockUsers.TryGetValue(accessToken, out var userInfo))
        {
            var claims = new EntraTokenClaims
            {
                ObjectId = userInfo.ObjectId,
                Email = userInfo.Email,
                GivenName = userInfo.FirstName,
                FamilyName = userInfo.LastName,
                Name = userInfo.DisplayName ?? $"{userInfo.FirstName} {userInfo.LastName}"
            };
            return Task.FromResult(Result<EntraTokenClaims>.Success(claims));
        }

        // Unknown token - treat as invalid
        return Task.FromResult(Result<EntraTokenClaims>.Failure(
            "Invalid access token"));
    }

    public Task<Result<EntraUserInfo>> GetUserInfoAsync(string accessToken)
    {
        // Check if service is disabled
        if (!IsEnabled)
        {
            return Task.FromResult(Result<EntraUserInfo>.Failure(
                "Entra External ID authentication is not enabled"));
        }

        // Validate input
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Task.FromResult(Result<EntraUserInfo>.Failure(
                "Access token is required"));
        }

        // Check for expired tokens
        if (_expiredTokens.Contains(accessToken))
        {
            return Task.FromResult(Result<EntraUserInfo>.Failure(
                "Token has expired"));
        }

        // Check for invalid tokens
        if (_invalidTokens.Contains(accessToken))
        {
            return Task.FromResult(Result<EntraUserInfo>.Failure(
                "Invalid access token"));
        }

        // Check for known test users
        if (_mockUsers.TryGetValue(accessToken, out var userInfo))
        {
            return Task.FromResult(Result<EntraUserInfo>.Success(userInfo));
        }

        // Unknown token - treat as invalid
        return Task.FromResult(Result<EntraUserInfo>.Failure(
            "Invalid access token"));
    }

    /// <summary>
    /// Add a custom test user for specific test scenarios
    /// </summary>
    public void AddTestUser(string token, EntraUserInfo userInfo)
    {
        _mockUsers[token] = userInfo;
    }

    /// <summary>
    /// Mark a token as expired for testing expiration scenarios
    /// </summary>
    public void AddExpiredToken(string token)
    {
        _expiredTokens.Add(token);
    }

    /// <summary>
    /// Mark a token as invalid for testing validation scenarios
    /// </summary>
    public void AddInvalidToken(string token)
    {
        _invalidTokens.Add(token);
    }

    /// <summary>
    /// Reset all mock data to initial state
    /// </summary>
    public void Reset()
    {
        _mockUsers.Clear();
        _expiredTokens.Clear();
        _invalidTokens.Clear();
        IsEnabled = true;
        InitializeTestUsers();
    }
}
