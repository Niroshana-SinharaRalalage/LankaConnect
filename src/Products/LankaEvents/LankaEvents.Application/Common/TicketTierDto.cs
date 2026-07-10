namespace LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;

/// <summary>
/// DTO representing a ticket tier for multi-tier events.
/// Maps from TicketTier domain entity.
/// </summary>
public record TicketTierDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal AdultPriceAmount { get; init; }
    public Currency AdultPriceCurrency { get; init; } = null!;
    public decimal? ChildPriceAmount { get; init; }
    public Currency? ChildPriceCurrency { get; init; }
    public int? ChildAgeLimit { get; init; }
    public bool HasChildPricing { get; init; }
    public int Capacity { get; init; }
    public int ReservedCount { get; init; }
    public int AvailableQuantity { get; init; }
    public int MaxPerUser { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; }
    public bool IsFree { get; init; }
}
