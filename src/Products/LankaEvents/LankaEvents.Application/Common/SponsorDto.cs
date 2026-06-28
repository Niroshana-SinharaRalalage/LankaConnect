namespace LankaConnect.Products.LankaEvents.Application.Common;

public record SponsorDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid? SponsorUserId { get; init; }
    public string SponsorName { get; init; } = null!;
    public string SponsorEmail { get; init; } = null!;
    public string? SponsorPhone { get; init; }
    public string? SponsorOrganization { get; init; }
    public string? SponsorNotes { get; init; }
    public string SponsorType { get; init; } = null!;
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public string Status { get; init; } = null!;
    public decimal? StripeFeeAmount { get; init; }
    public decimal? PlatformCommissionAmount { get; init; }
    public decimal? OrganizerPayoutAmount { get; init; }
    public string? ItemName { get; init; }
    public string? ItemDescription { get; init; }
    public decimal? EstimatedValue { get; init; }
    // Phase 6A.145 — optional sponsor image (LOGO). Any sponsor can attach an image (no threshold).
    public string? ImageUrl { get; init; }
    public string? ImageBlobName { get; init; }
    // Phase 6A.162 — optional sponsor brochure/flyer (sibling to logo). Orthogonal slot;
    // touching one does NOT mutate the other (pinned by SponsorTests independence invariants).
    public string? BrochureUrl { get; init; }
    public string? BrochureBlobName { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentCompletedAt { get; init; }
}
