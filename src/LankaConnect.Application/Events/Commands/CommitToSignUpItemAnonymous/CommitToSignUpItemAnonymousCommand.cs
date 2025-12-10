using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItemAnonymous;

/// <summary>
/// Command to commit to a sign-up item for anonymous users (not logged in)
/// Phase 6A.23: Supports anonymous sign-up workflow
/// The user must be registered for the event (verified by email) but doesn't need to be logged in
/// </summary>
public record CommitToSignUpItemAnonymousCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    string ContactEmail,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactPhone = null
) : ICommand<Guid>;
