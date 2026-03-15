namespace LankaConnect.Application.Events.Common;

public record CollectionDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid? ContributorUserId { get; init; }
    public string ContributorName { get; init; } = null!;
    public string ContributorEmail { get; init; } = null!;
    public string? ContributorPhone { get; init; }
    public string? ContributorNotes { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal? StripeFeeAmount { get; init; }
    public decimal? PlatformCommissionAmount { get; init; }
    public decimal? OrganizerPayoutAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentCompletedAt { get; init; }
}
