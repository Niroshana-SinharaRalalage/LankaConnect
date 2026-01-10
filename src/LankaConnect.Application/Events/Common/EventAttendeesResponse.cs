using System;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Response containing all attendees for an event with summary statistics.
/// Phase 6A.71: Added commission-aware revenue properties.
/// </summary>
public class EventAttendeesResponse
{
    public Guid EventId { get; init; }
    public string EventTitle { get; init; } = string.Empty;
    public List<EventAttendeeDto> Attendees { get; init; } = new();
    public int TotalRegistrations { get; init; }
    public int TotalAttendees { get; init; }

    // Phase 6A.71: Commission-aware revenue properties
    /// <summary>
    /// Total revenue before commission deduction (sum of all registration amounts).
    /// </summary>
    public decimal GrossRevenue { get; init; }

    /// <summary>
    /// Platform commission amount (LankaConnect + Stripe combined).
    /// </summary>
    public decimal CommissionAmount { get; init; }

    /// <summary>
    /// Net revenue after commission deduction (organizer's payout).
    /// </summary>
    public decimal NetRevenue { get; init; }

    /// <summary>
    /// Commission rate applied (e.g., 0.05 for 5%).
    /// </summary>
    public decimal CommissionRate { get; init; }

    /// <summary>
    /// Whether this is a free event (no revenue to track).
    /// </summary>
    public bool IsFreeEvent { get; init; }

    /// <summary>
    /// Legacy total revenue field (use GrossRevenue instead).
    /// </summary>
    [Obsolete("Use GrossRevenue instead. Will be removed in future version.")]
    public decimal? TotalRevenue { get; init; }
}
