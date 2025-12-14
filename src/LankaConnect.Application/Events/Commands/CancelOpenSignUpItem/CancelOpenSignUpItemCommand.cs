using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Command to cancel (delete) a user-submitted Open item
/// Only the user who created the item can cancel it
/// </summary>
public record CancelOpenSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid ItemId,
    Guid UserId // Must match the CreatedByUserId to allow cancellation
) : ICommand;
