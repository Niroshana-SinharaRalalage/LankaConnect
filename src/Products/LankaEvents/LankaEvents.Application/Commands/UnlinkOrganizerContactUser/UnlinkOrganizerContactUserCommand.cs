using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UnlinkOrganizerContactUser;

/// <summary>
/// Phase 6A.133: Unlink a user from an organizer contact, removing co-organizer access.
/// </summary>
public record UnlinkOrganizerContactUserCommand(
    Guid EventId,
    Guid ContactId
) : ICommand;
