using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.AddSignUpListToEvent;

public record AddSignUpListToEventCommand(
    Guid EventId,
    string Category,
    string Description,
    SignUpType SignUpType,
    List<string>? PredefinedItems = null
) : ICommand;
