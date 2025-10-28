namespace LankaConnect.Domain.Users.Enums;

/// <summary>
/// Represents the identity provider used for user authentication
/// </summary>
public enum IdentityProvider
{
    /// <summary>
    /// Local authentication with email/password stored in LankaConnect database
    /// </summary>
    Local = 0,

    /// <summary>
    /// Microsoft Entra External ID authentication (formerly Azure AD B2C)
    /// </summary>
    EntraExternal = 1
}

public static class IdentityProviderExtensions
{
    /// <summary>
    /// Gets the display name for the identity provider
    /// </summary>
    public static string ToDisplayName(this IdentityProvider provider)
    {
        return provider switch
        {
            IdentityProvider.Local => "Local",
            IdentityProvider.EntraExternal => "Microsoft Entra External ID",
            _ => provider.ToString()
        };
    }

    /// <summary>
    /// Determines if the provider requires a password hash to be stored
    /// </summary>
    public static bool RequiresPasswordHash(this IdentityProvider provider)
    {
        return provider == IdentityProvider.Local;
    }

    /// <summary>
    /// Determines if the provider requires an external provider ID
    /// </summary>
    public static bool RequiresExternalProviderId(this IdentityProvider provider)
    {
        return provider == IdentityProvider.EntraExternal;
    }

    /// <summary>
    /// Determines if the provider is an external identity provider
    /// </summary>
    public static bool IsExternalProvider(this IdentityProvider provider)
    {
        return provider == IdentityProvider.EntraExternal;
    }

    /// <summary>
    /// Determines if the provider pre-verifies user emails
    /// </summary>
    public static bool EmailPreVerified(this IdentityProvider provider)
    {
        return provider == IdentityProvider.EntraExternal;
    }
}
