using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.AddOpenSignUpItem;

/// <summary>
/// Phase 6A.27: Command to add a user-submitted Open item to a sign-up list
/// The user who creates the item automatically commits to bringing it
/// </summary>
public record AddOpenSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid UserId,
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null
) : ICommand<Guid>; // Returns the created sign-up item ID
