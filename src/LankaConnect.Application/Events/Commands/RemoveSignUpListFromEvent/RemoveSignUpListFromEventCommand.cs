using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RemoveSignUpListFromEvent;

public record RemoveSignUpListFromEventCommand(
    Guid EventId,
    Guid SignUpListId
) : ICommand;
