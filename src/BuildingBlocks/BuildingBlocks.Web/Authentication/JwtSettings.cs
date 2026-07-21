namespace LankaConnect.BuildingBlocks.Web.Authentication;

/// <summary>
/// Bound to the configuration section consumed by
/// <see cref="JwtAuthenticationExtensions.AddBuildingBlocksJwtAuthentication"/>.
/// Default section name: <c>Jwt</c>.
/// </summary>
/// <remarks>
/// All three string properties are required; the extension throws
/// <see cref="InvalidOperationException"/> at startup if any is missing.
/// Defaults align with the existing <c>LankaConnect.Hosts.AllInOne</c> JWT settings so
/// modules adopting this can drop in without changing their <c>appsettings.json</c>.
/// </remarks>
public sealed class JwtSettings
{
    /// <summary>Default configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Symmetric signing key. Must be at least 256 bits for HS256.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Expected issuer (iss claim).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Expected audience (aud claim).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Whether to require HTTPS for token transmission. Defaults to <c>true</c> —
    /// production should never run with this disabled.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Clock skew tolerance for token expiry checks. Defaults to <see cref="TimeSpan.Zero"/>.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.Zero;
}
