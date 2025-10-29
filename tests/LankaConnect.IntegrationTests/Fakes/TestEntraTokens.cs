namespace LankaConnect.IntegrationTests.Fakes;

/// <summary>
/// Test token constants for Entra External ID integration testing
/// These tokens map to specific test scenarios in FakeEntraExternalIdService
/// </summary>
public static class TestEntraTokens
{
    /// <summary>
    /// Valid token for auto-provisioning a new user
    /// Maps to: newuser@example.com (entra-oid-new-user-12345)
    /// </summary>
    public const string ValidTokenNewUser = "mock-valid-entra-token-new-user";

    /// <summary>
    /// Valid token for existing user login
    /// Maps to: existinguser@example.com (entra-oid-existing-user-67890)
    /// </summary>
    public const string ValidTokenExistingUser = "mock-valid-entra-token-existing-user";

    /// <summary>
    /// Valid token for testing profile update sync
    /// Maps to: profileupdate@example.com (entra-oid-profile-update-11111)
    /// </summary>
    public const string ValidTokenProfileUpdate = "mock-valid-entra-token-refresh";

    /// <summary>
    /// Valid token but email conflicts with existing local user
    /// Maps to: duplicate@example.com (entra-oid-duplicate-22222)
    /// </summary>
    public const string ValidTokenDuplicateEmail = "mock-entra-token-duplicate-email";

    /// <summary>
    /// Expired token (should return 401 Unauthorized)
    /// </summary>
    public const string ExpiredToken = "mock-expired-entra-token";

    /// <summary>
    /// Invalid/malformed token (should return 401 Unauthorized)
    /// </summary>
    public const string InvalidToken = "invalid-token";
}
