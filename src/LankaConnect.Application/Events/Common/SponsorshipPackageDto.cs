namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6A.156 — read-only projection of a <see cref="LankaConnect.Domain.Events.SponsorshipPackage"/>.
/// Mirrors the shape used for AddOnDefinitionDto so the FE can render a package
/// card with the same layout primitives as an add-on card.
///
/// Image URL + blob name are surfaced because the organizer-side editor needs
/// both (display the image + know the blob to clear).
/// </summary>
public class SponsorshipPackageDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = "USD";
    public int? QuantityLimit { get; init; }
    public int QuantitySold { get; init; }
    public int? RemainingStock { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public string? ImageUrl { get; init; }
    public string? ImageBlobName { get; init; }
    public string? Tier { get; init; }
    public IReadOnlyList<string> Perks { get; init; } = Array.Empty<string>();
    public int IncludedTicketCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
