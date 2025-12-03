using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 6D: DTO for group pricing tier in tiered group pricing
/// Represents a single pricing tier (e.g., "1-2 attendees: $15/person" or "3+ attendees: $12/person")
/// </summary>
public record GroupPricingTierDto
{
    /// <summary>
    /// Minimum number of attendees for this tier (inclusive, starts at 1)
    /// </summary>
    public int MinAttendees { get; init; }

    /// <summary>
    /// Maximum number of attendees for this tier (inclusive)
    /// Null represents unlimited tier (e.g., "3+" tier)
    /// </summary>
    public int? MaxAttendees { get; init; }

    /// <summary>
    /// Price per person for this tier
    /// </summary>
    public decimal PricePerPerson { get; init; }

    /// <summary>
    /// Currency for the price (USD, LKR, etc.)
    /// </summary>
    public Currency Currency { get; init; }

    /// <summary>
    /// Display-friendly tier range (e.g., "1-2", "3-5", "6+")
    /// </summary>
    public string TierRange { get; init; } = string.Empty;
}
