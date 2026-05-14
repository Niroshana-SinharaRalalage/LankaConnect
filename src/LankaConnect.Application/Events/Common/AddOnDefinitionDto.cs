namespace LankaConnect.Application.Events.Common;

public record AddOnDefinitionDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = null!;
    public int? QuantityLimit { get; init; }
    public int QuantitySold { get; init; }
    public int? RemainingStock { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    // Phase 6A.143 — optional add-on image surfaced to FE for thumbnail rendering
    // in AddOnSelector (buyer) and AddOnsManagementTab (organizer). Both fields
    // are either both set or both null.
    public string? ImageUrl { get; init; }
    public string? ImageBlobName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
