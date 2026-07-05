using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
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
