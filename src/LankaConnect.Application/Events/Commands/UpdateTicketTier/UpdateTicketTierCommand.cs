using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Commands.UpdateTicketTier;

public record UpdateTicketTierCommand(
    Guid EventId,
    Guid TierId,
    string Name,
    string? Description,
    decimal AdultPriceAmount,
    Currency AdultPriceCurrency,
    decimal? ChildPriceAmount,
    Currency? ChildPriceCurrency,
    int? ChildAgeLimit,
    int Capacity,
    int MaxPerUser,
    int SortOrder
) : ICommand;
