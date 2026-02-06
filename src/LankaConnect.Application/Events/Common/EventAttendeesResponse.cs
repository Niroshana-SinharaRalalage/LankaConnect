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
    /// DEPRECATED: Use GrossRevenue for gross amount or NetRevenue for organizer payout.
    /// This property will be removed in a future version.
    /// </summary>
    public decimal? TotalRevenue { get; init; }

    // Phase 6A.X: Detailed revenue breakdown totals

    /// <summary>
    /// Total sales tax collected from all registrations.
    /// </summary>
    public decimal TotalSalesTax { get; init; }

    /// <summary>
    /// Total Stripe processing fees for all registrations (2.9% + $0.30 per transaction).
    /// </summary>
    public decimal TotalStripeFees { get; init; }

    /// <summary>
    /// Total platform commission for all registrations (2% of taxable amount).
    /// </summary>
    public decimal TotalPlatformCommission { get; init; }

    /// <summary>
    /// Total organizer payout after all deductions (tax, Stripe fees, platform commission).
    /// This should equal NetRevenue for new events with revenue breakdown.
    /// </summary>
    public decimal TotalOrganizerPayout { get; init; }

    /// <summary>
    /// Average sales tax rate applied across registrations (for display purposes).
    /// </summary>
    public decimal AverageTaxRate { get; init; }

    /// <summary>
    /// Whether this event has detailed revenue breakdown data.
    /// True for events created after Phase 6A.X implementation.
    /// </summary>
    public bool HasRevenueBreakdown { get; init; }
}
