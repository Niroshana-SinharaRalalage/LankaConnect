using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CommitToSignUp;

public record CommitToSignUpCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid UserId,
    string ItemDescription,
    int Quantity
) : ICommand;
