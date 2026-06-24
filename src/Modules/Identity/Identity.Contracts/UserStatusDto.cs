namespace LankaConnect.Modules.Identity.Contracts;

/// <summary>
/// Cross-module mirror of the User account status (Active / Locked / Deactivated).
/// Returned by IIdentityQueries surface; reflects the User aggregate's effective
/// admin state (IsActive + IsLocked composite).
/// </summary>
/// <remarks>
/// Wave 4.6.a (2026-06-24). Not a direct mirror of any single Domain enum --
/// this DTO collapses the User aggregate's IsActive + IsLocked + LockoutEnd state
/// into a single readable enum for cross-module consumers (admin pages, user
/// search results, etc.).
/// </remarks>
public enum UserStatusDto : byte
{
    Active = 0,
    Locked = 1,
    Deactivated = 2,
}
