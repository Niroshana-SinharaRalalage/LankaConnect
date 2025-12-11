using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Commands.AddPassToEvent;

public record AddPassToEventCommand(
    Guid EventId,
    string PassName,
    string PassDescription,
    decimal PriceAmount,
    Currency PriceCurrency,
    int TotalQuantity
) : ICommand;
