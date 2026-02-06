using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.AddOpenSignUpItemAnonymous;

/// <summary>
/// Command to add a user-submitted Open item to a sign-up list for anonymous users
/// Phase 6A.44: Supports anonymous users adding Open items if they're registered for the event
/// The user who creates the item automatically commits to bringing it
/// </summary>
public record AddOpenSignUpItemAnonymousCommand(
    Guid EventId,
    Guid SignUpListId,
    string ContactEmail,
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactPhone = null
) : ICommand<Guid>; // Returns the created sign-up item ID
