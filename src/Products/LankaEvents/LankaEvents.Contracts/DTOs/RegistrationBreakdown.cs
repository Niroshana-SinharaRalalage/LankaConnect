using LankaConnect.Products.LankaEvents.Domain.Enums;
namespace LankaConnect.Products.LankaEvents.Contracts.DTOs; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt.

/// <summary>
/// Phase 7F-E (architect-approved 2026-05-01): single shared projection of "what does a
/// registration look like to a human" — consumed by the Ticket PDF, confirmation email,
/// event-detail "You're Registered!" card, and the RSVP form.
///
/// Architect-mandated shape (edit #1): <see cref="BreakdownPair.Captured"/> makes the
/// "N/A" decision a property of the data, not the renderer. Every surface renders
/// uniformly: <c>Captured == true</c> → render <c>Left/Right</c> values; <c>Captured ==
/// false</c> → render "N/A".
///
/// This replaces the email-only flat-string pattern in the legacy
/// <c>HeadCountEmailFormatter</c> (which stays as a thin compatibility shim until Slice
/// 7F-E.3 deletes it).
/// </summary>
public sealed record RegistrationBreakdown(
    IReadOnlyList<RegistrationBreakdownRow> Rows,
    int TotalAttendees,
    RegistrationMode Mode,
    bool IsTiered,
    // Phase 7F-E.6.A (architect-approved 2026-05-04): on multi-tier B-mode breakdowns
    // the per-tier rows can't carry registration-level demographics (architect Phase
    // 7F-C §2.2 #4 deferred per-tier gender storage). When the registration DID
    // capture demographics at the registration level (e.g. operator's B4 RSVP storing
    // {AM:2, AF:2, CM:2, CF:2} across two tiers), this Totals row surfaces them
    // honestly without lying about per-tier precision. Null when not multi-tier OR
    // when no axis is captured.
    RegistrationBreakdownTotals? Totals = null);

/// <summary>
/// One row per tier (or one row total for non-tiered registrations). Contains the
/// per-row count + age + gender breakdown.
/// </summary>
public sealed record RegistrationBreakdownRow(
    string? TierName,             // null = non-tiered (single row for the whole registration)
    int Count,
    BreakdownPair Age,            // Captured iff B2 / B4 / Mode A
    BreakdownPair Gender);        // Captured iff B3 / B4 / Mode A

/// <summary>
/// Phase 7F-E.6.A (architect-approved 2026-05-04): registration-level demographics
/// surface for multi-tier B-mode breakdowns. Carries only the demographic axes — the
/// tier list is in <see cref="RegistrationBreakdown.Rows"/>, the count is in
/// <see cref="RegistrationBreakdown.TotalAttendees"/>. Renderers display this as a
/// "Total" row at the bottom of the per-tier list.
/// </summary>
public sealed record RegistrationBreakdownTotals(
    BreakdownPair Age,            // Captured iff registration mode collected age
    BreakdownPair Gender);        // Captured iff registration mode collected gender

/// <summary>
/// A two-part demographic split (e.g. Adult/Child or Male/Female). The
/// <see cref="Captured"/> flag tells renderers whether the registration mode collected
/// this axis at all — surfaces show "N/A" when <see cref="Captured"/> is false.
///
/// When <see cref="Captured"/> is false, <see cref="Left"/> and <see cref="Right"/>
/// MUST be zero so buggy renderers that ignore the flag don't display stale numbers.
/// </summary>
public sealed record BreakdownPair(
    bool Captured,
    int Left,
    int Right,
    string LeftLabel,
    string RightLabel)
{
    public static BreakdownPair NotCaptured(string leftLabel, string rightLabel) =>
        new(Captured: false, Left: 0, Right: 0, LeftLabel: leftLabel, RightLabel: rightLabel);

    public static BreakdownPair AgeNotCaptured() => NotCaptured("Adult", "Child");
    public static BreakdownPair GenderNotCaptured() => NotCaptured("Male", "Female");

    public static BreakdownPair CapturedAge(int adults, int children) =>
        new(Captured: true, Left: adults, Right: children, LeftLabel: "Adult", RightLabel: "Child");

    public static BreakdownPair CapturedGender(int males, int females) =>
        new(Captured: true, Left: males, Right: females, LeftLabel: "Male", RightLabel: "Female");
}
