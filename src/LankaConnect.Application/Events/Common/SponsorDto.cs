namespace LankaConnect.Application.Events.Common;

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
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentCompletedAt { get; init; }
}
