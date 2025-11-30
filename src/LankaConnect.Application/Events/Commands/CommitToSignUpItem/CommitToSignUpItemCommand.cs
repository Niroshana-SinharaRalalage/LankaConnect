using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItem;

public record CommitToSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId,
    Guid UserId,
    int Quantity,
    string? Notes = null
) : ICommand;
