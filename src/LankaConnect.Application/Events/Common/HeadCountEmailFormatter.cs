using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 7E.4: Shared formatter that turns a Mode-B <see cref="HeadCountBreakdown"/>
/// into the human-readable strings the email templates expect.
///
/// Reused across <c>RegistrationConfirmedEventHandler</c>,
/// <c>AnonymousRegistrationConfirmedEventHandler</c>, <c>RegistrationCancelledEventHandler</c>,
/// <c>EventCancelledEventHandler</c>, <c>EventReminderJob</c>, and
/// <c>AttendeesAddedEventHandler</c> — anywhere the new
/// <see cref="LankaConnect.Shared.Email.Contracts.EmailTemplateContract.FlexibleRegistration"/>
/// constants need to be populated from a registration.
///
/// Why a single helper: the formatting must be identical across every email surface or the
/// user will see "2 adults · 1 child" in one mail and "2A 1C" in another. Centralising it
/// makes the contract: one breakdown → one line.
/// </summary>
public static class HeadCountEmailFormatter
{
    /// <summary>
    /// Computes the four mode-aware booleans + three pre-rendered strings for a given
    /// registration. For Mode A (Attendees populated) returns the "DetailedAttendees" shape;
    /// for Mode B (HeadCount populated) returns the head-count shape. The caller assigns the
    /// returned values to the corresponding <c>FreeEventRegistrationEmailParams</c> /
    /// <c>RegistrationCancellationEmailParams</c> / etc. fields.
    /// </summary>
    public static FlexibleRegistrationDisplay Compute(Registration registration)
    {
        // Mode A path: detailed attendees populated.
        var hasDetailed = registration.RegistrationMode == RegistrationMode.DetailedAttendees
                          && registration.Attendees.Any();
        var hasHeadCount = registration.HeadCount != null;

        // Phase 7F-E.3: build the structured per-tier HTML fragment via the shared
        // RegistrationBreakdownEmailRenderer. Same shape across 5 templates.
        // Mode A: derive from attendees. Mode B: derive from HeadCount.
        string registrationBreakdownHtml = string.Empty;
        try
        {
            RegistrationBreakdown? bd = null;
            if (registration.HeadCount != null)
            {
                bd = RegistrationBreakdownFormatter.FromHeadCount(
                    registration.HeadCount, registration.RegistrationMode);
            }
            else if (registration.Attendees.Any())
            {
                bd = RegistrationBreakdownFormatter.FromAttendees(registration.Attendees);
            }
            if (bd != null)
            {
                registrationBreakdownHtml = RegistrationBreakdownEmailRenderer.Render(
                    bd, leadAttendeeName: registration.LeadAttendeeName);
            }
        }
        catch
        {
            // Defensive fail-soft per architect: never block the email pipeline. If the
            // renderer throws (shouldn't happen given input invariants), the legacy
            // flat-line tokens still cover the visible block.
            registrationBreakdownHtml = string.Empty;
        }

        if (!hasHeadCount)
        {
            // Mode A (or legacy single-attendee).
            return new FlexibleRegistrationDisplay(
                hasDetailedAttendees: hasDetailed,
                hasHeadCount: false,
                hasHeadCountBreakdown: false,
                hasTierBreakdown: false,
                headCountTotal: string.Empty,
                headCountBreakdownLine: string.Empty,
                tierBreakdownLine: string.Empty,
                leadAttendeeName: string.Empty,
                registrationBreakdownHtml: registrationBreakdownHtml);
        }

        // Mode B path.
        var hc = registration.HeadCount!;
        var demographicLine = FormatDemographicLine(hc.Demographics);
        var tierLine = FormatTierLine(hc.TierCounts);

        return new FlexibleRegistrationDisplay(
            hasDetailedAttendees: false,
            hasHeadCount: true,
            hasHeadCountBreakdown: !string.IsNullOrEmpty(demographicLine),
            hasTierBreakdown: !string.IsNullOrEmpty(tierLine),
            headCountTotal: hc.Total.ToString(),
            headCountBreakdownLine: demographicLine,
            tierBreakdownLine: tierLine,
            leadAttendeeName: registration.LeadAttendeeName ?? string.Empty,
            registrationBreakdownHtml: registrationBreakdownHtml);
    }

    /// <summary>
    /// Builds the demographic line for a Mode B registration.
    ///
    /// Output examples:
    /// - B1 (no demographic axis): "" (empty)
    /// - B2 ByAge: "2 adults · 1 child"
    /// - B3 ByGender: "2 males · 1 female"
    /// - B4 ByAgeAndGender: "1 adult male · 1 adult female · 0 child males · 1 child female"
    ///
    /// Singular/plural handled per leaf. The middle dot separator matches the existing
    /// Phase 7C.2 location-decomposition style ("Address · City") so the visual rhythm
    /// is consistent across the email body.
    /// </summary>
    public static string FormatDemographicLine(DemographicBreakdown? demographics)
    {
        if (demographics == null) return string.Empty;
        var parts = new List<string>();

        // Pick the populated axis. We only emit the leaves that are non-null AND > 0,
        // to avoid "0 adults · 0 children" eyesores when only one age category was used.
        // For B4 (4-leaf), we DO emit all four even if zero — to make the matrix legible.
        var hasFourLeaves = demographics.AdultMales.HasValue || demographics.AdultFemales.HasValue
                            || demographics.ChildMales.HasValue || demographics.ChildFemales.HasValue;

        if (hasFourLeaves)
        {
            AddLeaf(parts, demographics.AdultMales, "adult male", "adult males", emitZero: true);
            AddLeaf(parts, demographics.AdultFemales, "adult female", "adult females", emitZero: true);
            AddLeaf(parts, demographics.ChildMales, "child male", "child males", emitZero: true);
            AddLeaf(parts, demographics.ChildFemales, "child female", "child females", emitZero: true);
        }
        else
        {
            // B2: Adults / Children
            AddLeaf(parts, demographics.Adults, "adult", "adults", emitZero: false);
            AddLeaf(parts, demographics.Children, "child", "children", emitZero: false);
            // B3: Males / Females
            AddLeaf(parts, demographics.Males, "male", "males", emitZero: false);
            AddLeaf(parts, demographics.Females, "female", "females", emitZero: false);
        }

        return string.Join(" · ", parts);
    }

    /// <summary>
    /// Builds the tier line. Mode-aware per Phase 7F-C architect edit #11:
    /// <list type="bullet">
    /// <item>Legacy (<see cref="TierCount.HasAgeSplit"/> == false): <c>"VIP × 2, General × 3"</c></item>
    /// <item>With age split: <c>"VIP: 2 adults · 1 child, General: 5 adults"</c></item>
    /// </list>
    /// Mixed lists (some tiers with age split, some without) are not allowed by domain
    /// invariants, so this method assumes all-or-nothing across <paramref name="tierCounts"/>.
    /// Tier name is the SNAPSHOT from registration time (architect-required) — survives
    /// downstream tier rename/delete on the underlying event.
    /// </summary>
    public static string FormatTierLine(IReadOnlyList<TierCount>? tierCounts)
    {
        if (tierCounts == null || tierCounts.Count == 0) return string.Empty;
        return string.Join(", ", tierCounts.Select(FormatSingleTier));
    }

    private static string FormatSingleTier(TierCount tc)
    {
        if (!tc.HasAgeSplit)
            return $"{tc.TierName} × {tc.Count}";

        // Age-split path: "VIP: 2 adults · 1 child" — only emit non-zero leaves so that
        // an all-adult tier still reads "VIP: 3 adults" (not "VIP: 3 adults · 0 children").
        var parts = new List<string>();
        var adults = tc.AdultCount ?? 0;
        var children = tc.ChildCount ?? 0;
        if (adults > 0)
            parts.Add($"{adults} {(adults == 1 ? "adult" : "adults")}");
        if (children > 0)
            parts.Add($"{children} {(children == 1 ? "child" : "children")}");

        // Defence: HasAgeSplit==true but both zero → emit "0 adults" so the line is informative.
        if (parts.Count == 0)
            parts.Add("0 adults");

        return $"{tc.TierName}: {string.Join(" · ", parts)}";
    }

    private static void AddLeaf(List<string> parts, int? value, string singular, string plural, bool emitZero)
    {
        if (!value.HasValue) return;
        if (value.Value == 0 && !emitZero) return;
        var word = value.Value == 1 ? singular : plural;
        parts.Add($"{value.Value} {word}");
    }
}

/// <summary>
/// Phase 7E.4: Result struct for <see cref="HeadCountEmailFormatter.Compute"/> — carries
/// the seven values that map 1:1 onto the <c>EmailTemplateContract.FlexibleRegistration</c>
/// constants plus the lead attendee name.
/// </summary>
public sealed record FlexibleRegistrationDisplay(
    bool hasDetailedAttendees,
    bool hasHeadCount,
    bool hasHeadCountBreakdown,
    bool hasTierBreakdown,
    string headCountTotal,
    string headCountBreakdownLine,
    string tierBreakdownLine,
    string leadAttendeeName,
    string registrationBreakdownHtml);
