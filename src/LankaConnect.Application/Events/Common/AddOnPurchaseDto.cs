namespace LankaConnect.Application.Events.Common;

public record AddOnPurchaseDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid AddOnDefinitionId { get; init; }
    public string AddOnName { get; init; } = null!;
    public Guid? RegistrationId { get; init; }
    public Guid? BuyerUserId { get; init; }
    public string BuyerName { get; init; } = null!;
    public string BuyerEmail { get; init; } = null!;
    public string? BuyerPhone { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal? StripeFeeAmount { get; init; }
    public decimal? PlatformCommissionAmount { get; init; }
    public decimal? OrganizerPayoutAmount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? PaymentCompletedAt { get; init; }
}
