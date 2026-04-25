using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Commands.RemoveTicketTier;

public record RemoveTicketTierCommand(
    Guid EventId,
    Guid TierId
) : ICommand;
