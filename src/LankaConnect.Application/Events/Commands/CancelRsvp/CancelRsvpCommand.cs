using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelRsvp;

/// <summary>
/// Command to cancel a user's event registration
/// Phase 6A.28: Added DeleteSignUpCommitments parameter to give users choice
/// </summary>
/// <param name="EventId">The event to cancel registration for</param>
/// <param name="UserId">The user cancelling their registration</param>
/// <param name="DeleteSignUpCommitments">
/// If true, deletes all sign-up commitments and restores remaining quantities.
/// If false (default), keeps sign-up commitments intact.
/// </param>
public record CancelRsvpCommand(
    Guid EventId,
    Guid UserId,
    bool DeleteSignUpCommitments = false
) : ICommand;
