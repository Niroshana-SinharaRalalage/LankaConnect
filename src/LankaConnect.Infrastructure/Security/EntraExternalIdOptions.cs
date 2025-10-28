namespace LankaConnect.Infrastructure.Security;

/// <summary>
/// Configuration options for Microsoft Entra External ID integration
/// Maps to appsettings.json EntraExternalId section
/// </summary>
public class EntraExternalIdOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "EntraExternalId";

    /// <summary>
    /// Azure AD Tenant ID (e.g., 369a3c47-33b7-4baa-98b8-6ddf16a51a31)
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application (client) ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD Application (client) Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Tenant domain (e.g., lankaconnect.onmicrosoft.com)
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD instance (e.g., https://login.microsoftonline.com/)
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Callback path for OIDC authentication (e.g., /signin-oidc)
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Whether Entra External ID authentication is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Token validation parameters
    /// </summary>
    public TokenValidationOptions TokenValidation { get; set; } = new();
}

/// <summary>
/// Token validation configuration for Entra External ID
/// </summary>
public class TokenValidationOptions
{
    /// <summary>
    /// Validate the token signature
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Validate the token issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate the token audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Validate the token lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance in seconds (default: 300 = 5 minutes)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 300;
}
