namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// DTO for a single donation record.
/// </summary>
public record DonationDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid? RegistrationId { get; init; }
    public Guid? DonorUserId { get; init; }
    public string DonorName { get; init; } = null!;
    public string DonorEmail { get; init; } = null!;
    public string? DonorPhone { get; init; }
    public string? DonorNotes { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = null!;
    public string Status { get; init; } = null!;
    public bool IsBundled { get; init; }

    // Revenue breakdown
    public decimal? StripeFeeAmount { get; init; }
    public decimal? PlatformCommissionAmount { get; init; }
    public decimal? OrganizerPayoutAmount { get; init; }

    // Timestamps
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentCompletedAt { get; init; }
}
