using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// Represents the federated social identity provider used through Azure Entra External ID
/// All social logins go through Entra External ID, which federates to these upstream providers
/// The 'idp' claim in Entra tokens identifies which federated provider was used
/// </summary>
public enum FederatedProvider
{
    /// <summary>
    /// Direct Microsoft Entra External ID authentication (no federation)
    /// idp claim: "login.microsoftonline.com"
    /// </summary>
    Microsoft = 0,

    /// <summary>
    /// Facebook authentication federated through Entra External ID
    /// idp claim: "facebook.com"
    /// </summary>
    Facebook = 1,

    /// <summary>
    /// Google authentication federated through Entra External ID
    /// idp claim: "google.com"
    /// </summary>
    Google = 2,

    /// <summary>
    /// Apple authentication federated through Entra External ID
    /// idp claim: "appleid.apple.com"
    /// </summary>
    Apple = 3
}

/// <summary>
/// Extension methods for FederatedProvider enum
/// </summary>
public static class FederatedProviderExtensions
{
    /// <summary>
    /// Gets the display name for the federated provider
    /// </summary>
    public static string ToDisplayName(this FederatedProvider provider)
    {
        return provider switch
        {
            FederatedProvider.Microsoft => "Microsoft",
            FederatedProvider.Facebook => "Facebook",
            FederatedProvider.Google => "Google",
            FederatedProvider.Apple => "Apple",
            _ => provider.ToString()
        };
    }

    /// <summary>
    /// Gets the Entra 'idp' claim value for this federated provider
    /// This is the value you'll find in the JWT token's 'idp' claim
    /// </summary>
    public static string ToIdpClaimValue(this FederatedProvider provider)
    {
        return provider switch
        {
            FederatedProvider.Microsoft => "login.microsoftonline.com",
            FederatedProvider.Facebook => "facebook.com",
            FederatedProvider.Google => "google.com",
            FederatedProvider.Apple => "appleid.apple.com",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }

    /// <summary>
    /// Parses the Entra 'idp' claim value to determine the federated provider
    /// Case-insensitive matching for robustness
    /// </summary>
    /// <param name="idpClaimValue">The value from the 'idp' claim in the Entra token</param>
    /// <returns>Result containing the FederatedProvider or an error if claim is invalid</returns>
    public static Result<FederatedProvider> FromIdpClaimValue(string? idpClaimValue)
    {
        if (string.IsNullOrWhiteSpace(idpClaimValue))
            return Result<FederatedProvider>.Failure("Invalid identity provider claim: value is null or empty");

        // Case-insensitive matching for robustness
        var claim = idpClaimValue.Trim().ToLowerInvariant();

        return claim switch
        {
            "login.microsoftonline.com" => Result<FederatedProvider>.Success(FederatedProvider.Microsoft),
            "facebook.com" => Result<FederatedProvider>.Success(FederatedProvider.Facebook),
            "google.com" => Result<FederatedProvider>.Success(FederatedProvider.Google),
            "appleid.apple.com" => Result<FederatedProvider>.Success(FederatedProvider.Apple),
            _ => Result<FederatedProvider>.Failure($"Invalid identity provider claim: '{idpClaimValue}' is not a recognized provider")
        };
    }
}
