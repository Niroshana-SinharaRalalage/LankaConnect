using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Queries.CheckEventRegistration;

/// <summary>
/// Query to check event registration status by email with enhanced details
/// Phase 6A.23: Supports anonymous sign-up workflow with proper member detection
///
/// User Experience Flow:
/// 1. Check if email belongs to a LankaConnect member (User account)
///    - YES → Prompt to log in (IsMember = true)
///    - NO → Check event registration
/// 2. If not a member, check if registered for the event
///    - YES → Allow anonymous commitment
///    - NO → Prompt to register for event first
/// </summary>
public record CheckEventRegistrationQuery(
    Guid EventId,
    string Email
) : IQuery<EventRegistrationCheckResult>;

/// <summary>
/// Result of checking event registration by email
/// Phase 6A.23: Enhanced to support proper UX flow
/// </summary>
public record EventRegistrationCheckResult
{
    /// <summary>
    /// Whether the email belongs to a LankaConnect member (User account exists)
    /// If true, user should be prompted to log in instead of anonymous commitment
    /// </summary>
    public bool HasUserAccount { get; init; }

    /// <summary>
    /// The UserId if the email belongs to a member, null otherwise
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Whether the email is registered for this specific event
    /// </summary>
    public bool IsRegisteredForEvent { get; init; }

    /// <summary>
    /// The registration ID if registered for the event
    /// </summary>
    public Guid? RegistrationId { get; init; }

    /// <summary>
    /// Indicates if the user can proceed with anonymous commitment
    /// True only if: NOT a member AND registered for event
    /// </summary>
    public bool CanCommitAnonymously => !HasUserAccount && IsRegisteredForEvent;

    /// <summary>
    /// Indicates if the user should be prompted to log in
    /// True if: IS a member (has User account)
    /// </summary>
    public bool ShouldPromptLogin => HasUserAccount;

    /// <summary>
    /// Indicates if the user needs to register for the event first
    /// True if: NOT a member AND NOT registered for event
    /// </summary>
    public bool NeedsEventRegistration => !HasUserAccount && !IsRegisteredForEvent;

    /// <summary>
    /// Creates a result for an email that belongs to a LankaConnect member
    /// User should be prompted to log in
    /// </summary>
    public static EventRegistrationCheckResult MemberAccount(Guid userId, bool isRegisteredForEvent, Guid? registrationId) => new()
    {
        HasUserAccount = true,
        UserId = userId,
        IsRegisteredForEvent = isRegisteredForEvent,
        RegistrationId = registrationId
    };

    /// <summary>
    /// Creates a result for an anonymous user who is registered for the event
    /// User can proceed with anonymous commitment
    /// </summary>
    public static EventRegistrationCheckResult AnonymousRegistered(Guid registrationId) => new()
    {
        HasUserAccount = false,
        UserId = null,
        IsRegisteredForEvent = true,
        RegistrationId = registrationId
    };

    /// <summary>
    /// Creates a result for an anonymous user who is NOT registered for the event
    /// User needs to register for the event first
    /// </summary>
    public static EventRegistrationCheckResult AnonymousNotRegistered() => new()
    {
        HasUserAccount = false,
        UserId = null,
        IsRegisteredForEvent = false,
        RegistrationId = null
    };
}
