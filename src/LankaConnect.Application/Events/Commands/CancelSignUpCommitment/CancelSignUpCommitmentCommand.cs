using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.CancelSignUpCommitment;

public record CancelSignUpCommitmentCommand(
    Guid EventId,
    Guid SignUpListId,
    Guid UserId
) : ICommand;
