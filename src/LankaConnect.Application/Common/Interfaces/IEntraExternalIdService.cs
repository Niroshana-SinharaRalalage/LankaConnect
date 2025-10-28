using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for Microsoft Entra External ID authentication and token validation
/// </summary>
public interface IEntraExternalIdService
{
    /// <summary>
    /// Validates an Entra External ID access token
    /// </summary>
    /// <param name="accessToken">The JWT access token from Entra External ID</param>
    /// <returns>Result containing claims principal if validation succeeds</returns>
    Task<Result<EntraTokenClaims>> ValidateAccessTokenAsync(string accessToken);

    /// <summary>
    /// Retrieves user information from Entra External ID using the access token
    /// </summary>
    /// <param name="accessToken">The JWT access token</param>
    /// <returns>Result containing Entra user information</returns>
    Task<Result<EntraUserInfo>> GetUserInfoAsync(string accessToken);

    /// <summary>
    /// Checks if Entra External ID integration is enabled
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Entra External ID token claims
/// </summary>
public class EntraTokenClaims
{
    /// <summary>
    /// Unique identifier (OID claim) from Entra External ID
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's full name (optional)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// User's given name / first name (optional)
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// User's family name / last name (optional)
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// All claims from the token
    /// </summary>
    public Dictionary<string, string> AllClaims { get; set; } = new();
}

/// <summary>
/// User information from Entra External ID
/// </summary>
public class EntraUserInfo
{
    /// <summary>
    /// Unique identifier (OID) from Entra External ID
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's first name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// User's last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Whether the email is verified by Entra
    /// </summary>
    public bool EmailVerified { get; set; } = true; // Entra pre-verifies emails
}
