namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service to get the current authenticated user's information
/// Phase 6A.6: Notification System
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current user's ID
    /// Returns Guid.Empty if user is not authenticated
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Gets the current user's email
    /// Returns null if user is not authenticated
    /// </summary>
    string? UserEmail { get; }

    /// <summary>
    /// Indicates whether the user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }
}
