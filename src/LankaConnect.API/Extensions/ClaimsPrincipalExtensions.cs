using System.Security.Claims;

namespace LankaConnect.API.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get user ID from claims
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            throw new InvalidOperationException("User ID claim not found");
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new InvalidOperationException($"Invalid user ID format: {userIdClaim}");
        }

        return userId;
    }

    /// <summary>
    /// Try to get user ID from claims (returns null if not found or invalid)
    /// </summary>
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return null;
        }

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
