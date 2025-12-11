using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RemoveSignUpItem;

public record RemoveSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid SignUpItemId
) : ICommand;
