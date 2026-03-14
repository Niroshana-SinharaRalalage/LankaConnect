using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UnlinkOrganizerContactUser;

/// <summary>
/// Phase 6A.133: Unlink a user from an organizer contact, removing co-organizer access.
/// </summary>
public record UnlinkOrganizerContactUserCommand(
    Guid EventId,
    Guid ContactId
) : ICommand;
