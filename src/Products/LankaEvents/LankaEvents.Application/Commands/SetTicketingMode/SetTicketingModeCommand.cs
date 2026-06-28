using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Products.LankaEvents.Application.Commands.SetTicketingMode;

/// <summary>
/// Command to set the ticketing mode for an event (SingleTier or Tiered).
/// Must be set to Tiered before adding ticket tiers.
/// </summary>
public record SetTicketingModeCommand(
    Guid EventId,
    TicketingMode TicketingMode
) : ICommand;
