using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.SharedKernel.Money;
namespace LankaConnect.Products.LankaEvents.Application.Commands.AddTicketTier;

/// <summary>
/// Command to add a ticket tier to an event.
/// Event must be in Tiered ticketing mode.
/// </summary>
public record AddTicketTierCommand(
    Guid EventId,
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
) : ICommand<Guid>;  // Returns the new tier ID
