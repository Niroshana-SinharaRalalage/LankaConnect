using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItem;

/// <summary>
/// Command to commit to a sign-up item
/// Phase 2: Added optional contact information fields
/// </summary>
public record CommitToSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    Guid UserId,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null
) : ICommand;
