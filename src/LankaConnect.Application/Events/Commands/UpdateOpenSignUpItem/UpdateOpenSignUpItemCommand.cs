using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Command to update a user-submitted Open item
/// Can update item name, quantity, notes, and contact info
/// </summary>
public record UpdateOpenSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid ItemId,
    Guid UserId, // Must match the CreatedByUserId to allow update
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null
) : ICommand;
