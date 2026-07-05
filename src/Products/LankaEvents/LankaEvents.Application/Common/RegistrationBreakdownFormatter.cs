using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Application.Common;

/// <summary>
/// Phase 7F-E.1 (architect-approved 2026-05-01): shared projection of a registration
/// into the rendering-agnostic <see cref="RegistrationBreakdown"/> shape.
///
/// Promoted from the email-only <c>HeadCountEmailFormatter</c> so 4 surfaces (PDF,
/// confirmation email, event-detail card, RSVP form) can render uniformly. Renderer-
/// specific concerns (HTML wrappers, PDF cell layouts, JSX) live in each surface's
/// own renderer; this class only owns the data shape.
///
/// Two entry points:
/// - <see cref="FromHeadCount"/> for Mode B (B1/B2/B3/B4) registrations.
/// - <see cref="FromAttendees"/> for Mode A (DetailedAttendees) registrations.
///
/// Both produce the same <see cref="RegistrationBreakdown"/> shape, so consuming
/// surfaces never need to know which mode produced the breakdown.
/// </summary>
public static class RegistrationBreakdownFormatter
{
    /// <summary>
    /// Mode B (head-count) projection. The mode parameter determines which axes the
    /// formatter marks as <see cref="BreakdownPair.Captured"/>:
    /// - B1 (HeadCountOnly): both Age + Gender NotCaptured.
    /// - B2 (HeadCountByAge): Age captured, Gender NotCaptured.
    /// - B3 (HeadCountByGender): Gender captured, Age NotCaptured.
    /// - B4 (HeadCountByAgeAndGender): both Age + Gender captured (Gender derived from
    ///   the 4-leaf cross at the registration level — per-tier gender is NOT stored
    ///   per architect Phase 7F-C §2.2 #4).
    /// </summary>
    public static RegistrationBreakdown FromHeadCount(
        HeadCountBreakdown headCount, RegistrationMode mode)
    {
        if (headCount == null)
            throw new ArgumentNullException(nameof(headCount));

        var captureAge = mode is RegistrationMode.HeadCountByAge
                                  or RegistrationMode.HeadCountByAgeAndGender;
        var captureGender = mode is RegistrationMode.HeadCountByGender
                                     or RegistrationMode.HeadCountByAgeAndGender;

        var demo = headCount.Demographics;
        var aggregateAdults = captureAge ? AggregateAdults(demo, mode) : 0;
        var aggregateChildren = captureAge ? AggregateChildren(demo, mode) : 0;
        var aggregateMales = captureGender ? AggregateMales(demo, mode) : 0;
        var aggregateFemales = captureGender ? AggregateFemales(demo, mode) : 0;

        var rows = new List<RegistrationBreakdownRow>();
        var isTiered = headCount.TierCounts is { Count: > 0 };

        if (!isTiered)
        {
            rows.Add(BuildRow(
                tierName: null,
                count: headCount.Total,
                captureAge: captureAge,
                tierAdults: aggregateAdults,
                tierChildren: aggregateChildren,
                captureGender: captureGender,
                tierMales: aggregateMales,
                tierFemales: aggregateFemales));
        }
        else
        {
            // Per-tier rows: when the tier has its own age split (Phase 7F-C), use it.
            // Otherwise (legacy tier with null age axis) fall back to the row-level
            // demographic only when there's a SINGLE tier (the demographics apply to
            // the only tier unambiguously). For multi-tier without per-tier age split,
            // age is reported as NotCaptured at the row level — the demographic line
            // applies to the registration as a whole, not per-tier.
            var tiers = headCount.TierCounts!;
            var singleTier = tiers.Count == 1;

            foreach (var tc in tiers)
            {
                int rowAdults = 0, rowChildren = 0;
                bool rowAgeCaptured;

                if (tc.HasAgeSplit)
                {
                    rowAgeCaptured = captureAge;
                    rowAdults = tc.AdultCount ?? 0;
                    rowChildren = tc.ChildCount ?? 0;
                }
                else if (singleTier && captureAge)
                {
                    // Legacy path: single-tier registration's row-level age comes from
                    // the registration-level demographic line.
                    rowAgeCaptured = true;
                    rowAdults = aggregateAdults;
                    rowChildren = aggregateChildren;
                }
                else
                {
                    rowAgeCaptured = false;
                }

                // Gender: was deferred per architect Phase 7F-C §2.2 #4 (per-tier gender
                // not stored in the multi-tier path). Phase 7F-E.7 (architect-approved
                // 2026-05-04) re-opens that decision: TierCount now optionally carries a
                // per-tier 4-leaf split (HasFourLeafSplit), so when set we render per-tier
                // gender from the captured 4-leaf. For single-tier B3/B4, derive from
                // registration-level (legacy single-tier path unchanged). Multi-tier
                // without 4-leaf still falls through to NotCaptured + Totals row.
                int rowMales = 0, rowFemales = 0;
                bool rowGenderCaptured;
                if (tc.HasFourLeafSplit && captureGender)
                {
                    rowGenderCaptured = true;
                    rowMales = (tc.AdultMaleCount ?? 0) + (tc.ChildMaleCount ?? 0);
                    rowFemales = (tc.AdultFemaleCount ?? 0) + (tc.ChildFemaleCount ?? 0);
                }
                else if (singleTier && captureGender)
                {
                    rowGenderCaptured = true;
                    rowMales = aggregateMales;
                    rowFemales = aggregateFemales;
                }
                else
                {
                    rowGenderCaptured = false;
                }

                // Phase 7F-E.7: when 4-leaf is set, also override the age-row capture
                // to use the 4-leaf-derived adult/child counts so per-tier rows show the
                // user-entered values consistently. The 7F-C `HasAgeSplit` branch already
                // populates these from auto-derived AdultCount/ChildCount (set by the
                // factory when 4-leaf lands), so this is implicit — no extra branch.

                rows.Add(BuildRow(
                    tierName: tc.TierName,
                    count: tc.Count,
                    captureAge: rowAgeCaptured,
                    tierAdults: rowAdults,
                    tierChildren: rowChildren,
                    captureGender: rowGenderCaptured,
                    tierMales: rowMales,
                    tierFemales: rowFemales));
            }
        }

        // Phase 7F-E.6.A (architect-approved 2026-05-04): build a registration-level
        // Totals row for multi-tier B-mode breakdowns when at least one demographic
        // axis was captured at registration level but the per-tier rows DON'T already
        // carry the captured demographics.
        //
        // Phase 7F-E.7 update (architect-approved 2026-05-04): when ALL per-tier rows
        // already show captured demographics on every captured axis (because the
        // 4-leaf is set per-tier), the Totals row would just duplicate the per-tier
        // sums — architect "Totals row optionally redundant" → skip it. This keeps the
        // legacy/partial-coverage path working (where some tiers have 4-leaf and some
        // don't, OR no tiers do) and avoids a duplicate row on the new fully-captured
        // path.
        //
        // Single-tier breakdowns already carry the demographics in Rows[0]; non-tiered
        // breakdowns have a single row that's the whole registration. In both cases
        // the Totals row is a misleading duplicate, so we skip it.
        RegistrationBreakdownTotals? totals = null;
        if (isTiered && rows.Count > 1 && (captureAge || captureGender))
        {
            // Phase 7F-E.7: skip Totals row when per-tier rows fully cover the captured
            // axes. "Fully cover" = for each captured axis, every per-tier row shows
            // Captured=true.
            var ageFullyCovered = !captureAge || rows.All(r => r.Age.Captured);
            var genderFullyCovered = !captureGender || rows.All(r => r.Gender.Captured);
            var allCovered = ageFullyCovered && genderFullyCovered;

            if (!allCovered)
            {
                totals = new RegistrationBreakdownTotals(
                    Age: captureAge
                        ? BreakdownPair.CapturedAge(aggregateAdults, aggregateChildren)
                        : BreakdownPair.AgeNotCaptured(),
                    Gender: captureGender
                        ? BreakdownPair.CapturedGender(aggregateMales, aggregateFemales)
                        : BreakdownPair.GenderNotCaptured());
            }
        }

        return new RegistrationBreakdown(
            Rows: rows,
            TotalAttendees: headCount.Total,
            Mode: mode,
            IsTiered: isTiered,
            Totals: totals);
    }

    /// <summary>
    /// Mode A (DetailedAttendees) projection. Walks the per-attendee list, groups by
    /// <see cref="AttendeeDetails.TicketTierId"/>, and counts age / gender per group.
    ///
    /// Captured semantics: Age is captured iff every attendee has an
    /// <see cref="AgeCategory"/> (always true today since the field is required).
    /// Gender is captured iff at least one attendee has a non-null
    /// <see cref="AttendeeDetails.Gender"/> — registrations created without gender data
    /// (legacy path) are reported as NotCaptured rather than zero/zero.
    /// </summary>
    public static RegistrationBreakdown FromAttendees(IReadOnlyList<AttendeeDetails> attendees)
    {
        if (attendees == null)
            throw new ArgumentNullException(nameof(attendees));

        var attList = attendees.ToList();
        if (attList.Count == 0)
        {
            return new RegistrationBreakdown(
                Rows: Array.Empty<RegistrationBreakdownRow>(),
                TotalAttendees: 0,
                Mode: RegistrationMode.DetailedAttendees,
                IsTiered: false);
        }

        var anyTier = attList.Any(a => a.TicketTierId.HasValue);
        var anyGender = attList.Any(a => a.Gender.HasValue);

        var rows = new List<RegistrationBreakdownRow>();

        if (!anyTier)
        {
            // Single row for the whole registration.
            rows.Add(RowFromAttendees(tierName: null, attList, anyGenderInRow: anyGender));
        }
        else
        {
            // One row per tier. Group by TicketTierId, preserve TicketTierName from
            // the first attendee in each group (snapshot semantics).
            var groups = attList
                .Where(a => a.TicketTierId.HasValue)
                .GroupBy(a => new { a.TicketTierId, a.TicketTierName })
                .ToList();

            foreach (var group in groups)
            {
                var groupGender = group.Any(a => a.Gender.HasValue);
                rows.Add(RowFromAttendees(
                    tierName: group.Key.TicketTierName ?? "Unknown",
                    group.ToList(),
                    anyGenderInRow: groupGender));
            }

            // Defensive: attendees without a tier (shouldn't happen on a tiered event,
            // but if it does, lump them under "Other").
            var noTier = attList.Where(a => !a.TicketTierId.HasValue).ToList();
            if (noTier.Count > 0)
                rows.Add(RowFromAttendees("Other", noTier, anyGenderInRow: noTier.Any(a => a.Gender.HasValue)));
        }

        return new RegistrationBreakdown(
            Rows: rows,
            TotalAttendees: attList.Count,
            Mode: RegistrationMode.DetailedAttendees,
            IsTiered: anyTier);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Internal helpers
    // ──────────────────────────────────────────────────────────────────────

    private static RegistrationBreakdownRow BuildRow(
        string? tierName, int count,
        bool captureAge, int tierAdults, int tierChildren,
        bool captureGender, int tierMales, int tierFemales)
    {
        return new RegistrationBreakdownRow(
            TierName: tierName,
            Count: count,
            Age: captureAge
                ? BreakdownPair.CapturedAge(tierAdults, tierChildren)
                : BreakdownPair.AgeNotCaptured(),
            Gender: captureGender
                ? BreakdownPair.CapturedGender(tierMales, tierFemales)
                : BreakdownPair.GenderNotCaptured());
    }

    private static RegistrationBreakdownRow RowFromAttendees(
        string? tierName, IReadOnlyList<AttendeeDetails> attendees, bool anyGenderInRow)
    {
        var adults = attendees.Count(a => a.AgeCategory == AgeCategory.Adult);
        var children = attendees.Count(a => a.AgeCategory == AgeCategory.Child);
        var males = attendees.Count(a => a.Gender == Gender.Male);
        var females = attendees.Count(a => a.Gender == Gender.Female);

        return new RegistrationBreakdownRow(
            TierName: tierName,
            Count: attendees.Count,
            Age: BreakdownPair.CapturedAge(adults, children),
            Gender: anyGenderInRow
                ? BreakdownPair.CapturedGender(males, females)
                : BreakdownPair.GenderNotCaptured());
    }

    private static int AggregateAdults(DemographicBreakdown? demo, RegistrationMode mode) => mode switch
    {
        RegistrationMode.HeadCountByAge => demo?.Adults ?? 0,
        RegistrationMode.HeadCountByAgeAndGender => (demo?.AdultMales ?? 0) + (demo?.AdultFemales ?? 0),
        _ => 0,
    };

    private static int AggregateChildren(DemographicBreakdown? demo, RegistrationMode mode) => mode switch
    {
        RegistrationMode.HeadCountByAge => demo?.Children ?? 0,
        RegistrationMode.HeadCountByAgeAndGender => (demo?.ChildMales ?? 0) + (demo?.ChildFemales ?? 0),
        _ => 0,
    };

    private static int AggregateMales(DemographicBreakdown? demo, RegistrationMode mode) => mode switch
    {
        RegistrationMode.HeadCountByGender => demo?.Males ?? 0,
        RegistrationMode.HeadCountByAgeAndGender => (demo?.AdultMales ?? 0) + (demo?.ChildMales ?? 0),
        _ => 0,
    };

    private static int AggregateFemales(DemographicBreakdown? demo, RegistrationMode mode) => mode switch
    {
        RegistrationMode.HeadCountByGender => demo?.Females ?? 0,
        RegistrationMode.HeadCountByAgeAndGender => (demo?.AdultFemales ?? 0) + (demo?.ChildFemales ?? 0),
        _ => 0,
    };
}
