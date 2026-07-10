using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Money;
namespace LankaConnect.Products.LankaEvents.Application.Commands.UpdateTicketTier;

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
