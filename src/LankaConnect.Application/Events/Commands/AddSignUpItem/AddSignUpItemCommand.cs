using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.AddSignUpItem;

public record AddSignUpItemCommand(
    Guid EventId,
    Guid SignUpListId,
    string ItemDescription,
    int Quantity,
    SignUpItemCategory ItemCategory,
    string? Notes = null
) : ICommand<Guid>; // Returns the newly created SignUpItem ID
