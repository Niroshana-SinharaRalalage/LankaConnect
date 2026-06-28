namespace LankaConnect.Products.LankaEvents.Application.Queries.GetActiveSponsorshipPackages;

/// <summary>
/// Phase 6A.157 — public-facing DTO for buyer-visible sponsorship package
/// listings. Strips organizer-only fields from the full
/// <see cref="LankaConnect.Application.Events.Common.SponsorshipPackageDto"/>:
///   - Removed: <c>QuantitySold</c> (internal accounting), <c>QuantityLimit</c>
///     (only show <c>RemainingStock</c> to buyers), <c>ImageBlobName</c>
///     (Azure internal), <c>CreatedAt</c>/<c>UpdatedAt</c> (no buyer reason
///     to see audit), <c>IsActive</c> (server-filtered — only Active rows
///     appear in this list).
///   - Kept: id, eventId, name, description, price, currency, remainingStock
///     (null = unlimited), isSoldOut (computed), sortOrder, imageUrl, tier,
///     perks (informational), includedTicketCount (informational only — no
///     ticket issuance per 6A.157 final scope).
/// </summary>
public class SponsorshipPackagePublicDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; } = "USD";

    /// <summary>
    /// Phase 6A.157 — buyer-visible remaining stock. Null means unlimited
    /// (organizer set no limit). Zero means sold out (also flagged via
    /// <see cref="IsSoldOut"/>). Server-filtered list never returns sold-out
    /// packages, but the field is exposed so the FE can update inline if a
    /// purchase races to the last slot between page load and click.
    /// </summary>
    public int? RemainingStock { get; init; }

    public bool IsSoldOut { get; init; }
    public int SortOrder { get; init; }
    public string? ImageUrl { get; init; }
    public string? Tier { get; init; }
    public List<string> Perks { get; init; } = new();

    /// <summary>
    /// Phase 6A.157 — informational only. Per user pivot 2026-05-31, the
    /// system does NOT issue tickets for package sponsors. Organizer handles
    /// admission off-platform. FE displays this as a gray info note when > 0.
    /// </summary>
    public int IncludedTicketCount { get; init; }
}
