using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.UpdateSignUpItem;

/// <summary>
/// Command to update sign-up item details (description, quantity, and notes)
/// Phase 6A.14: Edit Sign-Up Item feature
/// </summary>
public record UpdateSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    string ItemDescription,
    int Quantity,
    string? Notes
) : ICommand;
