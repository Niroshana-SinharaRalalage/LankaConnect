using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 7E: Per-event registration capture mode (DetailedAttendees / HeadCount* / NoRegistration).
/// Default <see cref="RegistrationMode.DetailedAttendees"/> preserves all pre-7E event behaviour.
/// </summary>
public partial class Event
{
    /// <summary>
    /// Phase 7E: Organiser-selected registration capture shape.
    /// - <see cref="RegistrationMode.DetailedAttendees"/> = today's per-attendee registration (default).
    /// - <see cref="RegistrationMode.HeadCountOnly"/> through <c>HeadCountByAgeAndGender"/> = head-count modes.
    /// - <see cref="RegistrationMode.NoRegistration"/> = drop-in event; standalone donations/sponsors/add-ons/collections still work.
    ///
    /// Persisted with DB-level <c>DEFAULT 0</c> so legacy events materialise as <see cref="RegistrationMode.DetailedAttendees"/>
    /// automatically (Phase 6A.123 lesson).
    /// </summary>
    public RegistrationMode RegistrationMode { get; private set; } = RegistrationMode.DetailedAttendees;

    /// <summary>
    /// Phase 7E: Sets the registration mode for this event.
    ///
    /// Business rules:
    /// 1. Mode change is forbidden once <see cref="Registrations"/>.Any() — protects historical data.
    ///    Mode A↔B conversion with attendee backfill is deferred to Phase 7F.
    /// 2. Standalone contributions (donations / sponsors / add-on purchases / collections) are
    ///    intentionally NOT considered by this guard. They are mode-agnostic by design — their
    ///    aggregates live outside the <c>Event.Registrations</c> collection (verified in 7E.0
    ///    audit §6: Event has no <c>Donations</c>/<c>Sponsors</c>/<c>AddOnPurchases</c>/<c>Collections</c>
    ///    navigation collections; only nullable <c>*Configuration</c> value-objects).
    /// 3. Compatibility with pricing / seating / add-on shapes is enforced at the application layer
    ///    via <c>FluentValidation</c> in 7E.2 (the 14-row compatibility table). This domain method
    ///    only enforces the registration-locking rule.
    /// </summary>
    /// <param name="mode">The new registration mode.</param>
    /// <returns>Result indicating success or failure with a clear message.</returns>
    public Result SetRegistrationMode(RegistrationMode mode)
    {
        // Architect rule (Phase 7E plan §3.2): forbid mode change while ACTIVE registrations
        // exist — those are the rows that would need attendee backfill on A↔B conversion.
        // Cancelled / Refunded / Abandoned registrations are historical-only: they don't
        // consume capacity and have nothing to backfill. Counting them caused the bug where
        // an event showing "0 registered / 75 spots left" on the dashboard refused mode-change
        // with "Existing registrations: 30" — the dashboard's CurrentRegistrations and the
        // mode-change guard were using different definitions of "exists".
        //
        // Active = anything not in {Cancelled, Refunded, Abandoned}.
        // - Confirmed / Waitlisted / CheckedIn / Attended: live, would need backfill.
        // - Preliminary: awaiting payment; if it completes, it'd carry the wrong mode shape.
        // - RefundRequested: still consumes capacity until refund completes.
        // - Pending (deprecated): treat like Preliminary for safety.
#pragma warning disable CS0618 // Pending is deprecated but still excluded for back-compat.
        var activeRegistrations = _registrations
            .Where(r => r.Status != RegistrationStatus.Cancelled &&
                        r.Status != RegistrationStatus.Refunded &&
                        r.Status != RegistrationStatus.Abandoned)
            .ToList();
#pragma warning restore CS0618

        if (activeRegistrations.Count > 0)
        {
            // Phase 7E follow-up: surface the blocking status breakdown so the organiser can act.
            // The dashboard's CurrentRegistrations only shows Confirmed → if the dashboard says
            // "0 registered" but this guard fires, the row is in an intermediate state
            // (Preliminary stuck Stripe checkout / Waitlisted / RefundRequested awaiting webhook).
            var byStatus = string.Join(", ",
                activeRegistrations
                    .GroupBy(r => r.Status)
                    .OrderBy(g => g.Key.ToString())
                    .Select(g => $"{g.Count()} {g.Key}"));

            return Result.Failure(
                $"Cannot change registration mode while active registrations exist " +
                $"({byStatus}). Cancel or wait for these to resolve before changing the mode. " +
                $"Mode change with attendee backfill is deferred to Phase 7F. " +
                $"EventId={Id}, CurrentMode={RegistrationMode}, RequestedMode={mode}");
        }

        if (RegistrationMode == mode)
        {
            return Result.Success(); // Idempotent — no change to make.
        }

        // Phase 8X.11 — RegistrationMode.External is a paired axis with EventPaymentMode.ExternalPaid.
        // Switching INTO External requires the event to already be ExternalPaid (the proper
        // path to flip TO ExternalPaid is SetExternalPayment, which sets the mode atomically).
        // Switching OUT OF External is fine here — the picker change is the user's intent.
        if (mode == RegistrationMode.External && PaymentMode != EventPaymentMode.ExternalPaid)
        {
            return Result.Failure(
                "External registration mode requires PaymentMode = ExternalPaid. " +
                "Set the event to 'Paid — external registration link' first, then choose External mode.");
        }

        // Phase 8X.11 — ExternalPaid events are pinned to External mode. The organiser must
        // first switch payment mode (to Free or OnPlatformPaid) before picking a different
        // registration mode. Reject mode change here so the form's intent is unambiguous.
        if (PaymentMode == EventPaymentMode.ExternalPaid && mode != RegistrationMode.External)
        {
            return Result.Failure(
                "External-paid events use the External registration mode. " +
                "Change the payment mode to Free or 'Paid — collected on LankaConnect' first " +
                "if you want to capture internal attendee details.");
        }

        // Slice S1.5 invariant: AssignedSeating requires DetailedAttendees. Block any
        // mode change that would orphan an existing assigned-seating layout (Mode B/C
        // can't bind seats to attendees). Organiser must revert seating mode to GA
        // first, then change registration mode.
        if (SeatingMode == SeatingMode.AssignedSeating
            && mode != RegistrationMode.DetailedAttendees)
        {
            return Result.Failure(
                $"Cannot change registration mode to {mode} while assigned seating is enabled. " +
                $"Assigned seating requires individual-attendee registration. " +
                $"Switch seating mode to General Admission first, then change registration mode.");
        }

        RegistrationMode = mode;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Phase 7F-B (architect-approved 2026-04-30): convert all *active* registrations on this
    /// event from one <see cref="RegistrationMode"/> to another, performing the appropriate
    /// attendee backfill at each registration row.
    ///
    /// Conversion semantics (per architect plan §2):
    /// - <strong>A → B</strong>: collapse the per-attendee list into a head-count + lead name +
    ///   demographic axis derived from per-attendee AgeCategory/Gender + per-tier-age axis
    ///   derived from per-attendee TicketTierId × AgeCategory (depends on 7F-C live).
    /// - <strong>B → A</strong>: explode the head-count into N placeholder
    ///   <see cref="AttendeeDetails"/> rows with deterministic ordering and name fabrication
    ///   (row 1 = unmodified <c>LeadAttendeeName</c>; rows 2..N = <c>{LeadName} (n)</c>).
    ///
    /// Skipped (per-registration, surfaced in the report):
    /// - Source carries <c>Gender.Other</c> attendees AND target is B3/B4 (those modes don't
    ///   model <c>Other</c>) → <c>GenderOtherNotSupportedByMode</c>.
    /// - Source carries an attendee with a named-seat assignment AND target is B → named seats
    ///   require Mode A's per-attendee identity → <c>NamedSeatsRequireDetailedAttendees</c>.
    /// - Registration is in <see cref="ConversionPolicy.RegistrationIdsWithPendingAdditions"/>
    ///   (set by handler) → <c>PendingAdditionMustResolveFirst</c>.
    ///
    /// Hard rejects (whole conversion fails):
    /// - Active registration count exceeds <see cref="ConversionPolicy.BatchCap"/> (architect Q7).
    /// - Currency / pricing / target-mode invariants violated by the request (e.g. <c>Total=0</c>
    ///   after Other-gender filtering).
    ///
    /// Idempotent on same-mode (no rows migrated, no rows skipped, returns empty report).
    /// Untouched: Cancelled / Refunded / Abandoned registrations stay in their original mode
    /// snapshot (their snapshot powers historical email re-renders; flipping it would corrupt them).
    ///
    /// When <see cref="ConversionPolicy.DryRun"/> is true, the method computes the report
    /// but does NOT mutate any registration or the event itself — the report is the basis
    /// for the UI's diff-preview confirmation dialog.
    /// </summary>
    public Result<ConversionReport> ConvertRegistrationMode(
        RegistrationMode targetMode, ConversionPolicy policy)
    {
        if (policy == null)
            return Result<ConversionReport>.Failure("ConversionPolicy is required");

        // Same-mode is a no-op (idempotent).
        if (targetMode == RegistrationMode)
            return Result<ConversionReport>.Success(ConversionReport.Empty);

        // Pre-condition #1: A↔B only in this slice. C-conversions still require zero active
        // registrations (architect §2.3 — current guard's behaviour preserved). If either side
        // is C, defer to SetRegistrationMode (which has the zero-reg guard).
        if (RegistrationMode == RegistrationMode.NoRegistration
            || targetMode == RegistrationMode.NoRegistration)
            return Result<ConversionReport>.Failure(
                "Conversions involving Mode C (NoRegistration) must use SetRegistrationMode and " +
                "require zero active registrations. Cancel or wait for active registrations to resolve first.");

#pragma warning disable CS0618 // Pending is deprecated but excluded for back-compat.
        var activeRegistrations = _registrations
            .Where(r => r.Status != RegistrationStatus.Cancelled
                        && r.Status != RegistrationStatus.Refunded
                        && r.Status != RegistrationStatus.Abandoned)
            .ToList();
#pragma warning restore CS0618

        if (activeRegistrations.Count > policy.BatchCap)
            return Result<ConversionReport>.Failure(
                $"Conversion batch size ({activeRegistrations.Count}) exceeds the cap ({policy.BatchCap}). " +
                "Split into multiple smaller batches — cancel or pre-resolve a subset first, then re-run.");

        var migrated = new List<MigratedRow>();
        var skipped = new List<SkippedRow>();

        foreach (var reg in activeRegistrations)
        {
            // Architect Q8 — pending RegistrationAddition.
            if (policy.RegistrationIdsWithPendingAdditions.Contains(reg.Id))
            {
                skipped.Add(new SkippedRow(reg.Id, "PendingAdditionMustResolveFirst",
                    "Registration has a pending add-attendees payment. Complete or cancel it before converting."));
                continue;
            }

            var convertResult = ConvertSingleRegistration(reg, targetMode, policy);
            if (convertResult.skipped is { } skip)
            {
                skipped.Add(skip);
                continue;
            }

            // Successful migration — apply it (unless DryRun).
            if (!policy.DryRun)
            {
                var applyResult = ApplySingleConversion(reg, convertResult.migration!);
                if (applyResult.IsFailure)
                    return Result<ConversionReport>.Failure(applyResult.Errors);
            }

            migrated.Add(convertResult.migration!);
        }

        // Flip the event's mode (only when not dry-run AND at least one migration applied OR
        // there were no active registrations to migrate at all).
        if (!policy.DryRun && (migrated.Count > 0 || activeRegistrations.Count == 0))
        {
            RegistrationMode = targetMode;
            MarkAsUpdated();
        }

        return Result<ConversionReport>.Success(new ConversionReport(migrated, skipped));
    }

    private (MigratedRow? migration, SkippedRow? skipped) ConvertSingleRegistration(
        Registration reg, RegistrationMode targetMode, ConversionPolicy policy)
    {
        // A → B
        if (reg.RegistrationMode == RegistrationMode.DetailedAttendees && IsHeadCountMode(targetMode))
            return ConvertAToB(reg, targetMode);

        // B → A
        if (IsHeadCountMode(reg.RegistrationMode) && targetMode == RegistrationMode.DetailedAttendees)
            return ConvertBToA(reg, targetMode);

        // B → B (same family or cross-family) — out of scope for 7F-B.1; architect plan only
        // covers A↔B. Skip rather than reject the whole batch.
        return (null, new SkippedRow(reg.Id, "BModeToBModeNotSupported",
            "B↔B conversion (e.g. B2→B4) is not supported in this slice. Convert to Mode A first, then to the target B mode."));
    }

    private static bool IsHeadCountMode(RegistrationMode mode) =>
        mode == RegistrationMode.HeadCountOnly
        || mode == RegistrationMode.HeadCountByAge
        || mode == RegistrationMode.HeadCountByGender
        || mode == RegistrationMode.HeadCountByAgeAndGender;

    private (MigratedRow? migration, SkippedRow? skipped) ConvertAToB(
        Registration reg, RegistrationMode targetMode)
    {
        // Per-registration named-seat check (architect §2.4 missing-edit). Mode B has no named-
        // seat concept; collapsing here would orphan the seat assignment.
        if (reg.Attendees.Any(a => a.SeatId.HasValue))
            return (null, new SkippedRow(reg.Id, "NamedSeatsRequireDetailedAttendees",
                "Registration has named-seat assignments which Mode B cannot represent. Re-seat as general admission first."));

        // Architect Q1: B3 / B4 don't model Gender.Other. Skip per registration.
        if ((targetMode == RegistrationMode.HeadCountByGender
             || targetMode == RegistrationMode.HeadCountByAgeAndGender)
            && reg.Attendees.Any(a => a.Gender == Enums.Gender.Other))
            return (null, new SkippedRow(reg.Id, "GenderOtherNotSupportedByMode",
                $"Registration has attendee(s) with Gender=Other; target mode {targetMode} does not capture this category."));

        var attendees = reg.Attendees.ToList();
        var total = attendees.Count;
        if (total <= 0)
            return (null, new SkippedRow(reg.Id, "NoAttendeesToConvert",
                "Registration has no attendees — nothing to migrate."));

        // Build per-tier-age axis (depends on 7F-C: TierCount.AdultCount/ChildCount).
        IReadOnlyList<TierCount>? tierCounts = null;
        if (TicketingMode == Enums.TicketingMode.Tiered && attendees.Any(a => a.TicketTierId.HasValue))
        {
            // Stable sort: by SortOrder ascending, fall back to TierName.
            var orderedTiers = _ticketTiers.OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToList();
            var tcList = new List<TierCount>();
            foreach (var tier in orderedTiers)
            {
                var inTier = attendees.Where(a => a.TicketTierId == tier.Id).ToList();
                if (inTier.Count == 0) continue;

                int? adultCount = null, childCount = null;
                // Populate per-tier-age axis when target mode captures age (B2 / B4).
                if (targetMode == RegistrationMode.HeadCountByAge
                    || targetMode == RegistrationMode.HeadCountByAgeAndGender)
                {
                    adultCount = inTier.Count(a => a.AgeCategory == Enums.AgeCategory.Adult);
                    childCount = inTier.Count(a => a.AgeCategory == Enums.AgeCategory.Child);
                }
                var tcResult = TierCount.Create(tier.Id, tier.Name, inTier.Count, adultCount, childCount);
                if (tcResult.IsFailure)
                    return (null, new SkippedRow(reg.Id, "TierAxisDerivationFailed",
                        $"Could not build tier-age axis for tier '{tier.Name}': {tcResult.Error}"));
                tcList.Add(tcResult.Value);
            }
            tierCounts = tcList;
        }

        // Build the head-count breakdown for the target mode.
        Result<HeadCountBreakdown> hcResult = targetMode switch
        {
            RegistrationMode.HeadCountOnly =>
                HeadCountBreakdown.ForTotalOnly(total, tierCounts),

            RegistrationMode.HeadCountByAge =>
                HeadCountBreakdown.ForByAge(
                    adults: attendees.Count(a => a.AgeCategory == Enums.AgeCategory.Adult),
                    children: attendees.Count(a => a.AgeCategory == Enums.AgeCategory.Child),
                    tierCounts),

            RegistrationMode.HeadCountByGender =>
                HeadCountBreakdown.ForByGender(
                    males: attendees.Count(a => a.Gender == Enums.Gender.Male),
                    females: attendees.Count(a => a.Gender == Enums.Gender.Female),
                    tierCounts),

            RegistrationMode.HeadCountByAgeAndGender =>
                HeadCountBreakdown.ForByAgeAndGender(
                    adultMales: attendees.Count(a => a.AgeCategory == Enums.AgeCategory.Adult && a.Gender == Enums.Gender.Male),
                    adultFemales: attendees.Count(a => a.AgeCategory == Enums.AgeCategory.Adult && a.Gender == Enums.Gender.Female),
                    childMales: attendees.Count(a => a.AgeCategory == Enums.AgeCategory.Child && a.Gender == Enums.Gender.Male),
                    childFemales: attendees.Count(a => a.AgeCategory == Enums.AgeCategory.Child && a.Gender == Enums.Gender.Female),
                    tierCounts),

            _ => Result<HeadCountBreakdown>.Failure($"Unsupported target mode: {targetMode}"),
        };

        if (hcResult.IsFailure)
            return (null, new SkippedRow(reg.Id, "HeadCountBreakdownConstructionFailed",
                $"Could not build head-count breakdown: {string.Join("; ", hcResult.Errors!)}"));

        // Lead name on A→B: use the first attendee's name. RegistrationContact has no
        // FullName field today (only Email / Phone / Address), so the architect-Q6 fallback
        // chain reduces to attendees[0].Name. If Contact ever gains a name field, prepend
        // that as the preferred source.
        var leadName = attendees[0].Name;

        return (new MigratedRow(
            RegistrationId: reg.Id,
            BeforeAttendees: attendees,
            BeforeHeadCount: null,
            AfterAttendees: null,
            AfterHeadCount: hcResult.Value,
            AfterLeadAttendeeName: leadName), null);
    }

    private (MigratedRow? migration, SkippedRow? skipped) ConvertBToA(
        Registration reg, RegistrationMode targetMode)
    {
        var hc = reg.HeadCount;
        if (hc == null)
            return (null, new SkippedRow(reg.Id, "MissingHeadCountOnSourceMode",
                "Registration is in Mode B but has no HeadCount populated — cannot explode into placeholders."));

        var leadName = reg.LeadAttendeeName ?? "Attendee";
        var placeholders = new List<AttendeeDetails>();

        // Placeholder allocation order — deterministic per architect §2.2:
        // 1. Sort tiers by SortOrder asc, then TierName asc.
        // 2. Within each tier (or if no tiers), allocate Adults before Children, Males before Females.
        var orderedTiers = new List<(Guid? tierId, string? tierName, int total, int? adult, int? child)>();
        if (hc.TierCounts == null)
        {
            orderedTiers.Add((null, null, hc.Total, null, null));
        }
        else
        {
            var withSort = hc.TierCounts.Select(tc =>
            {
                var liveTier = _ticketTiers.FirstOrDefault(t => t.Id == tc.TierId);
                return new
                {
                    TierId = (Guid?)tc.TierId,
                    TierName = (string?)(liveTier?.Name ?? tc.TierName),
                    Total = tc.Count,
                    Adult = tc.AdultCount,
                    Child = tc.ChildCount,
                    SortOrder = liveTier?.SortOrder ?? int.MaxValue,
                };
            });
            foreach (var x in withSort.OrderBy(x => x.SortOrder).ThenBy(x => x.TierName))
                orderedTiers.Add((x.TierId, x.TierName, x.Total, x.Adult, x.Child));
        }

        var rowIndex = 0;
        foreach (var (tierId, tierName, total, adultOrNull, childOrNull) in orderedTiers)
        {
            int adults, children;
            if (adultOrNull.HasValue)
            {
                adults = adultOrNull.Value;
                children = childOrNull ?? 0;
            }
            else if (reg.RegistrationMode == RegistrationMode.HeadCountByAge && hc.Demographics != null)
            {
                // Single-tier (or no-tier) B2 — derive from Demographics.
                adults = hc.Demographics.Adults ?? total;
                children = hc.Demographics.Children ?? 0;
            }
            else if (reg.RegistrationMode == RegistrationMode.HeadCountByAgeAndGender && hc.Demographics != null
                && hc.TierCounts == null)
            {
                adults = (hc.Demographics.AdultMales ?? 0) + (hc.Demographics.AdultFemales ?? 0);
                children = (hc.Demographics.ChildMales ?? 0) + (hc.Demographics.ChildFemales ?? 0);
            }
            else
            {
                // B1 / B3 (no age axis), or per-tier without an age split — treat all as adults.
                adults = total;
                children = 0;
            }

            // Fill in gender per leaf for B3 / B4. For B1 / B2 use null gender.
            // Allocation ratios for gender — pull from Demographics.
            int adultMales, adultFemales, childMales, childFemales;
            if (reg.RegistrationMode == RegistrationMode.HeadCountByGender && hc.Demographics != null)
            {
                // B3 has no age axis → treat all as Adult; split by gender.
                adultMales = hc.Demographics.Males ?? 0;
                adultFemales = hc.Demographics.Females ?? 0;
                adults = adultMales + adultFemales;
                children = 0;
                childMales = childFemales = 0;
            }
            else if (reg.RegistrationMode == RegistrationMode.HeadCountByAgeAndGender && hc.Demographics != null
                && hc.TierCounts == null)
            {
                adultMales = hc.Demographics.AdultMales ?? 0;
                adultFemales = hc.Demographics.AdultFemales ?? 0;
                childMales = hc.Demographics.ChildMales ?? 0;
                childFemales = hc.Demographics.ChildFemales ?? 0;
            }
            else
            {
                adultMales = adultFemales = childMales = childFemales = 0;
            }

            // Build attendee rows. Order: (Adult,Male)→(Adult,Female)→(Child,Male)→(Child,Female)
            // for B4; (Adult)→(Child) for B2; (Male)→(Female) for B3; flat for B1.
            void AppendRow(Enums.AgeCategory age, Enums.Gender? gender)
            {
                var name = rowIndex == 0 ? leadName : $"{leadName} ({rowIndex + 1})";
                var attendee = AttendeeDetails.Create(
                    name, age, gender,
                    ticketTierId: tierId,
                    ticketTierName: tierName);
                if (attendee.IsFailure)
                    throw new InvalidOperationException(
                        $"Could not synthesise placeholder for registration {reg.Id}: {attendee.Error}");
                placeholders.Add(attendee.Value);
                rowIndex++;
            }

            if (reg.RegistrationMode == RegistrationMode.HeadCountByAgeAndGender && hc.TierCounts == null)
            {
                for (var i = 0; i < adultMales; i++) AppendRow(Enums.AgeCategory.Adult, Enums.Gender.Male);
                for (var i = 0; i < adultFemales; i++) AppendRow(Enums.AgeCategory.Adult, Enums.Gender.Female);
                for (var i = 0; i < childMales; i++) AppendRow(Enums.AgeCategory.Child, Enums.Gender.Male);
                for (var i = 0; i < childFemales; i++) AppendRow(Enums.AgeCategory.Child, Enums.Gender.Female);
            }
            else if (reg.RegistrationMode == RegistrationMode.HeadCountByGender)
            {
                for (var i = 0; i < adultMales; i++) AppendRow(Enums.AgeCategory.Adult, Enums.Gender.Male);
                for (var i = 0; i < adultFemales; i++) AppendRow(Enums.AgeCategory.Adult, Enums.Gender.Female);
            }
            else
            {
                // B1, B2, or B-with-tier-age-axis — Adults first, then Children, no gender.
                for (var i = 0; i < adults; i++) AppendRow(Enums.AgeCategory.Adult, null);
                for (var i = 0; i < children; i++) AppendRow(Enums.AgeCategory.Child, null);
            }
        }

        return (new MigratedRow(
            RegistrationId: reg.Id,
            BeforeAttendees: null,
            BeforeHeadCount: hc,
            AfterAttendees: placeholders,
            AfterHeadCount: null,
            AfterLeadAttendeeName: null), null);
    }

    private static Result ApplySingleConversion(Registration reg, MigratedRow migration)
    {
        if (migration.AfterHeadCount != null)
        {
            // A → B
            return reg.ApplyConvertToHeadCountMode(
                ResolveTargetMode(migration), migration.AfterHeadCount, migration.AfterLeadAttendeeName);
        }
        if (migration.AfterAttendees != null)
        {
            // B → A
            return reg.ApplyConvertToDetailedAttendees(migration.AfterAttendees);
        }
        return Result.Failure("MigratedRow has neither AfterHeadCount nor AfterAttendees — programming error.");
    }

    private static RegistrationMode ResolveTargetMode(MigratedRow migration)
    {
        var hc = migration.AfterHeadCount!;
        // The target mode is encoded in the demographic shape we built.
        if (hc.Demographics == null) return RegistrationMode.HeadCountOnly;
        if (hc.Demographics.AdultMales.HasValue || hc.Demographics.AdultFemales.HasValue
            || hc.Demographics.ChildMales.HasValue || hc.Demographics.ChildFemales.HasValue)
            return RegistrationMode.HeadCountByAgeAndGender;
        if (hc.Demographics.Males.HasValue || hc.Demographics.Females.HasValue)
            return RegistrationMode.HeadCountByGender;
        return RegistrationMode.HeadCountByAge;
    }

    /// <summary>
    /// Phase 7E.3a: Registers a head-count (B-mode) RSVP for this event.
    ///
    /// Mirrors <see cref="RegisterWithAttendees"/> but builds a <see cref="HeadCountBreakdown"/>-backed
    /// registration instead of a per-attendee one. Performs the same status/date/duplicate guards
    /// and uses <see cref="HeadCountBreakdown.Total"/> for capacity checks.
    ///
    /// Scope discipline (7E.3a): free events ONLY. Paid B-mode RSVP — including Stripe checkout
    /// session creation and per-tier amount calculation — lands in 7E.3b. Calling this method
    /// against a paid event returns a clear failure pointing at 7E.3b.
    /// </summary>
    /// <param name="userId">Authenticated user ID, or null for anonymous.</param>
    /// <param name="leadAttendeeName">Name of the lead attendee — used in emails.</param>
    /// <param name="headCount">Composite head-count breakdown built via <see cref="HeadCountBreakdown"/> factories.</param>
    /// <param name="contact">Shared contact info — required for emails.</param>
    /// <returns>Success if the registration was created; failure with a clear message otherwise.</returns>
    public Result RegisterWithHeadCount(
        Guid? userId,
        string leadAttendeeName,
        HeadCountBreakdown headCount,
        RegistrationContact contact)
    {
        // 1. Status & date guards (same as RegisterWithAttendees).
        if (Status != EventStatus.Published)
            return Result.Failure("Cannot register for unpublished event");

        if (StartDate <= DateTime.UtcNow)
            return Result.Failure("Cannot register for an event that has already started");

        // Phase 8X.3 — ExternalPaid events have no internal registration path.
        // Same guard as RegisterWithAttendees (Event.cs).
        if (PaymentMode == Enums.EventPaymentMode.ExternalPaid)
            return Result.Failure(ExternalRegistrationGuardMessage);

        // Phase 8X.11 — defence-in-depth: RegistrationMode.External is a paired axis
        // with PaymentMode.ExternalPaid; reject if drifted.
        if (RegistrationMode == Enums.RegistrationMode.External)
            return Result.Failure(ExternalRegistrationGuardMessage);

        // 2. Mode guard (defensive — the handler also dispatches by mode, but we enforce here too).
        if (RegistrationMode == RegistrationMode.DetailedAttendees)
            return Result.Failure(
                "This event uses detailed-attendee registration. Use the per-attendee RSVP path.");

        if (RegistrationMode == RegistrationMode.NoRegistration)
            return Result.Failure(
                "Registration is not required for this event. Standalone donations / sponsors / " +
                "add-on purchases / collections are still accepted via their own endpoints.");

        // 3. Argument validation.
        if (string.IsNullOrWhiteSpace(leadAttendeeName))
            return Result.Failure("Lead attendee name is required for head-count registrations");

        if (headCount == null)
            return Result.Failure("Head-count breakdown is required");

        if (contact == null)
            return Result.Failure("Contact information is required");

        // 4. Duplicate registration check — mirror RegisterWithAttendees logic.
        // Phase 6A.XXX FIX: cross-path dup detection (UserId + email).
#pragma warning disable CS0618 // Pending is deprecated but still excluded for back-compat dup check.
        if (userId.HasValue)
        {
            var existingByUserId = _registrations.FirstOrDefault(r =>
                r.UserId == userId &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
            if (existingByUserId != null)
                return Result.Failure(
                    "You are already registered for this event. To change your registration, " +
                    "please cancel the existing one first.");

            var existingByEmail = _registrations.FirstOrDefault(r =>
                r.Contact != null &&
                r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase) &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
            if (existingByEmail != null)
                return Result.Failure(
                    "This email is already registered for this event. Each email can only register once.");
        }
        else
        {
            var existingByEmail = _registrations.FirstOrDefault(r =>
                ((r.Contact != null && r.Contact.Email.Equals(contact.Email, StringComparison.OrdinalIgnoreCase)) ||
                 (r.AttendeeInfo != null && r.AttendeeInfo.Email != null &&
                  r.AttendeeInfo.Email.Value.Equals(contact.Email, StringComparison.OrdinalIgnoreCase))) &&
                r.Status != RegistrationStatus.Cancelled &&
                r.Status != RegistrationStatus.Refunded &&
                r.Status != RegistrationStatus.RefundRequested &&
                r.Status != RegistrationStatus.Preliminary &&
                r.Status != RegistrationStatus.Abandoned &&
                r.Status != RegistrationStatus.Pending);
            if (existingByEmail != null)
                return Result.Failure(
                    "This email is already registered for this event. Each email can only register once.");
        }
#pragma warning restore CS0618

        // 5. MaxAttendeesPerRegistration guard — applies to head-count Total just as it does
        //    to Attendees.Count (per-architect: cap applies to both).
        var effectiveMax = Math.Min(MaxAttendeesPerRegistration, SYSTEM_MAX_ATTENDEES_PER_REGISTRATION);
        if (headCount.Total > effectiveMax)
            return Result.Failure(
                $"Maximum {effectiveMax} attendees per registration. Requested: {headCount.Total}.");

        // 6. Capacity guard — capacity check uses HeadCount.Total via Registration.GetAttendeeCount.
        if (!HasCapacityFor(headCount.Total))
            return Result.Failure(
                $"Event does not have enough capacity for {headCount.Total} attendees. " +
                $"Available: {Capacity - ReservedCapacity}.");

        // 7. Per-tier capacity reservation (Phase 7E.3c, architect edit #2): for tiered events
        //    with TierCounts, reserve capacity per tier BEFORE pricing. Mirrors Mode A's
        //    behaviour at Event.cs:444-451. Atomic — if any tier reservation fails the whole
        //    RSVP rejects (no partial reserve held). Applies to free + paid tiered events:
        //    even free tiered events need to prevent over-selling a tier.
        if (TicketingMode == Enums.TicketingMode.Tiered)
        {
            if (headCount.TierCounts == null || headCount.TierCounts.Count == 0)
                return Result.Failure(
                    "Tiered events require TierCounts on the head-count payload. " +
                    "Specify counts per tier (e.g. VIP × 2, General × 3).");

            // Validate all tier IDs exist before reserving — fail-fast prevents partial state.
            // Phase 7F-C: also reject any tier whose ChildPrice isn't configured but the
            // payload claims ChildCount > 0 — otherwise CalculatePriceForAttendee(Child) would
            // silently fall back to AdultPrice and the user would be UNDER-charged.
            foreach (var tc in headCount.TierCounts)
            {
                var matchedTier = _ticketTiers.FirstOrDefault(t => t.Id == tc.TierId);
                if (matchedTier == null)
                    return Result.Failure(
                        $"Ticket tier {tc.TierId} not found on this event. RegistrationId not yet created — no rollback needed.");

                if (tc.HasAgeSplit && (tc.ChildCount ?? 0) > 0 && !matchedTier.HasChildPricing)
                    return Result.Failure(
                        $"Tier '{matchedTier.Name}' has no child pricing configured but the registration " +
                        $"claims {tc.ChildCount} children in this tier. Either configure a ChildPrice on the tier " +
                        $"or remove the age split from this tier's count (children would otherwise be billed at AdultPrice).");
            }

            // Reserve atomically. If any reserve fails, prior reserves on this call are lost
            // (no rollback path exists today — same limitation Mode A has). For 7E.3c the
            // pre-validation above + the fail-fast on the first failure keeps blast radius small.
            foreach (var tc in headCount.TierCounts)
            {
                var tier = _ticketTiers.First(t => t.Id == tc.TierId);
                var reserveResult = tier.Reserve(tc.Count);
                if (reserveResult.IsFailure)
                    return Result.Failure(reserveResult.Errors);
            }
        }

        // 8. Pricing — Phase 7E.3b paid B-mode + Phase 7E.3c TierCounts. Free events get
        //    Money.Zero(USD); paid events get the appropriate pricing (single, dual, or
        //    tiered) based on the event's pricing configuration.
        var priceResult = CalculateHeadCountPrice(headCount);
        if (priceResult.IsFailure)
            return Result.Failure(priceResult.Errors);
        var totalPrice = priceResult.Value;

        var isPaidEvent = !IsFree();

        var registrationResult = Registration.CreateWithHeadCount(
            Id, userId, RegistrationMode,
            leadAttendeeName.Trim(),
            headCount, contact,
            totalPrice,
            isPaidEvent: isPaidEvent);

        if (registrationResult.IsFailure)
            return Result.Failure(registrationResult.Errors);

        _registrations.Add(registrationResult.Value);
        MarkAsUpdated();

        // 8. Raise domain events. For free events the registration goes straight to Confirmed
        //    and we raise the confirmation event here. For paid events the row is Preliminary
        //    until the Stripe webhook fires; CompletePayment() raises the confirmation event
        //    at that point. Mirrors Mode A's behaviour exactly (see RegisterWithAttendees).
        if (registrationResult.Value.Status == RegistrationStatus.Confirmed)
        {
            var attendeeCount = headCount.Total;
            if (userId.HasValue)
            {
                RaiseDomainEvent(new RegistrationConfirmedEvent(Id, userId.Value, attendeeCount, DateTime.UtcNow));
            }
            else
            {
                RaiseDomainEvent(new AnonymousRegistrationConfirmedEvent(Id, contact.Email, attendeeCount, DateTime.UtcNow));
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Phase 7E.3b: pricing helper for head-count (Mode B) registrations. Mirrors Mode A's
    /// <see cref="CalculatePriceForAttendees"/> shape but operates on the head-count breakdown
    /// instead of a per-attendee list.
    ///
    /// Pricing logic (matches plan §3 + architect review iteration 1):
    /// <list type="bullet">
    /// <item>Free event → <c>Money.Zero(USD)</c> (currency informational).</item>
    /// <item>Tiered ticketing → REJECT (<see cref="RegistrationModeErrorCodes.PaidHeadCountTiersDeferred"/>) until 7E.3c.</item>
    /// <item>TierCounts axis present → REJECT (same gate).</item>
    /// <item>GroupTiered pricing → <c>Pricing.CalculateGroupPrice(headCount.Total)</c> (per-architect: parity with Mode A).</item>
    /// <item>AgeDual + B2 (HeadCountByAge) → adults × adultPrice + children × childPrice.</item>
    /// <item>AgeDual + B4 (HeadCountByAgeAndGender) → (AM+AF) × adultPrice + (CM+CF) × childPrice.</item>
    /// <item>AgeDual + B1/B3 → REJECT defensively (validator already excludes; defence-in-depth).</item>
    /// <item>Standard / single price + any B → Total × ticketPrice.</item>
    /// </list>
    /// </summary>
    /// <summary>
    /// Phase 7F-D: public wrapper around <see cref="CalculateHeadCountPrice"/> so the
    /// application layer can compute the post-merge price for a Mode-B add-attendees flow
    /// (build the merged shape, call this method, diff against the registration's current
    /// TotalPrice). Stays a thin delegator — keeps the pricing rules in the domain.
    /// </summary>
    public Result<Money> CalculatePriceForHeadCount(HeadCountBreakdown headCount)
        => CalculateHeadCountPrice(headCount);

    private Result<Money> CalculateHeadCountPrice(HeadCountBreakdown headCount)
    {
        if (headCount == null)
            return Result<Money>.Failure("Head-count breakdown is required");

        // Free → zero. Currency is informational on a $0 registration.
        if (IsFree())
        {
            var zero = Money.Create(0m, Currency.USD);
            return zero.IsSuccess ? Result<Money>.Success(zero.Value) : Result<Money>.Failure(zero.Errors);
        }

        // Pricing-Guard Fix 2026-05-04: a paid event is well-formed when ANY of three
        // pricing shapes is configured (legacy Pricing, legacy TicketPrice, OR Tiered
        // with active tiers). Pre-fix this checked only the legacy shapes and rejected
        // paid+Tiered events created without a redundant SetDualPricing call (latent
        // bug exposed by 7F-E.4b operator browser test on event 616e59f3-...).
        // See HasPaidPricingConfigured() in Event.cs and EventPaidPricingGuardTests.
        if (!IsFreeEvent && !HasPaidPricingConfigured())
            return Result<Money>.Failure(
                "This event is marked as paid but has no pricing configured. " +
                "Add ticket tiers, set a ticket price, or mark the event as free before accepting registrations.");

        // Phase 7E.3c (2026-04-29): lifted the PaidHeadCountTiersDeferred gates. Tiered
        // ticketing now uses TierCounts pricing; standalone TierCounts (without tiered
        // ticketing) is still rejected because there's no tier price to look up.
        if (headCount.TierCounts != null && headCount.TierCounts.Count > 0
            && TicketingMode != Enums.TicketingMode.Tiered)
            return Result<Money>.Failure(
                "TierCounts can only be used with TicketingMode.Tiered events. " +
                "Remove TierCounts from the head-count payload, or configure tiered ticketing on the event.");

        // Tiered ticketing → use TierCounts pricing path. Free + tiered short-circuited
        // earlier (IsFree() returned zero); reaching here means paid + tiered.
        if (TicketingMode == Enums.TicketingMode.Tiered)
            return CalculateTierCountsPrice(headCount.TierCounts);

        // Group-tiered pricing — single tier price covers everyone in the basket; calculator
        // chooses the correct tier from Total.
        if (Pricing != null && Pricing.Type == Enums.PricingType.GroupTiered)
        {
            var groupResult = Pricing.CalculateGroupPrice(headCount.Total);
            return groupResult.IsSuccess
                ? Result<Money>.Success(groupResult.Value)
                : Result<Money>.Failure(groupResult.Error);
        }

        // Dual pricing (AgeDual): adults / children counts must be derivable from this mode.
        if (Pricing != null && Pricing.Type == Enums.PricingType.AgeDual)
        {
            int adults;
            int children;
            switch (RegistrationMode)
            {
                case RegistrationMode.HeadCountByAge:
                    var demoB2 = headCount.Demographics;
                    if (demoB2 == null)
                        return Result<Money>.Failure(
                            "HeadCountByAge requires a demographic breakdown with adults/children counts.");
                    adults = demoB2.Adults ?? 0;
                    children = demoB2.Children ?? 0;
                    break;

                case RegistrationMode.HeadCountByAgeAndGender:
                    var demoB4 = headCount.Demographics;
                    if (demoB4 == null)
                        return Result<Money>.Failure(
                            "HeadCountByAgeAndGender requires the 4-leaf demographic breakdown.");
                    adults = (demoB4.AdultMales ?? 0) + (demoB4.AdultFemales ?? 0);
                    children = (demoB4.ChildMales ?? 0) + (demoB4.ChildFemales ?? 0);
                    break;

                case RegistrationMode.HeadCountOnly:
                case RegistrationMode.HeadCountByGender:
                    // Defensive — validator excludes these combos. If reached, it's a bug.
                    return Result<Money>.Failure(
                        $"{RegistrationMode} cannot be used with dual pricing — adult/child counts are not " +
                        "captured by this mode. The compatibility validator should have rejected this combination.");

                default:
                    return Result<Money>.Failure(
                        $"Unsupported RegistrationMode for head-count pricing: {RegistrationMode}");
            }

            var adultMoney = Pricing.CalculateForCategory(Enums.AgeCategory.Adult);
            var childMoney = Pricing.CalculateForCategory(Enums.AgeCategory.Child);

            // Build the total: adults × adultPrice + children × childPrice.
            var adultsTotalResult = adultMoney.Multiply(adults);
            if (adultsTotalResult.IsFailure)
                return Result<Money>.Failure(adultsTotalResult.Errors);

            var childrenTotalResult = childMoney.Multiply(children);
            if (childrenTotalResult.IsFailure)
                return Result<Money>.Failure(childrenTotalResult.Errors);

            var sumResult = adultsTotalResult.Value.Add(childrenTotalResult.Value);
            return sumResult.IsSuccess
                ? Result<Money>.Success(sumResult.Value)
                : Result<Money>.Failure(sumResult.Errors);
        }

        // Standard / single price (Pricing.Type == Standard OR legacy TicketPrice fallback).
        var unitPrice = Pricing?.AdultPrice ?? TicketPrice;
        if (unitPrice == null)
            return Result<Money>.Failure("Event pricing is not configured");

        var multiplyResult = unitPrice.Multiply(headCount.Total);
        return multiplyResult.IsSuccess
            ? Result<Money>.Success(multiplyResult.Value)
            : Result<Money>.Failure(multiplyResult.Errors);
    }

    /// <summary>
    /// Phase 7F-C (architect-approved single-shape refactor 2026-04-30): TierCounts pricing
    /// for paid Mode-B RSVP. Mirrors Mode A's
    /// <see cref="Event.CalculateTieredPriceForAttendees"/> — routes through
    /// <see cref="TicketTier.CalculatePriceForAttendee"/> per attendee category, so adults
    /// pay <c>tier.AdultPrice</c> and children pay <c>tier.ChildPrice</c> (when the tier has
    /// child pricing configured).
    ///
    /// Single-shape derivation:
    /// <code>
    /// adultCount = tc.AdultCount ?? tc.Count   // legacy null-axis path → all "adults" for pricing
    /// childCount = tc.ChildCount ?? 0
    /// lineTotal  = tier.AdultPrice × adultCount + tier.CalculatePriceForAttendee(Child) × childCount
    /// </code>
    /// Legacy 7E.3c (B1 / B3 / B-without-age-split) registrations — where both
    /// <see cref="TierCount.AdultCount"/> and <see cref="TierCount.ChildCount"/> are null —
    /// keep producing <c>AdultPrice × Count</c> per architect Q7 ("legacy null-axis stays
    /// a valid choice indefinitely"). The architect-required Mode A vs Mode B parity test
    /// in <c>Phase7FCTierAgeMatrixPricingTests</c> asserts that the new shape produces the
    /// same Money for the same basket as Mode A.
    ///
    /// Pre-condition (architect edit #8 in 7F-C plan): <see cref="RegisterWithHeadCount"/>
    /// already rejected any (tier, ChildCount > 0) where the tier has no <c>ChildPrice</c>,
    /// so this method does not need to re-check — the silent under-charge cannot reach here.
    /// </summary>
    private Result<Money> CalculateTierCountsPrice(IReadOnlyList<TierCount>? tierCounts)
    {
        if (tierCounts == null || tierCounts.Count == 0)
            return Result<Money>.Failure(
                "TierCounts is required when TicketingMode is Tiered. " +
                "Specify counts per tier (e.g. VIP × 2, General × 3).");

        Money? total = null;
        foreach (var tc in tierCounts)
        {
            var tier = _ticketTiers.FirstOrDefault(t => t.Id == tc.TierId);
            if (tier == null)
                return Result<Money>.Failure(
                    $"Ticket tier {tc.TierId} not found on this event. " +
                    "TierCount has gone stale — verify the event's tier list before retrying.");

            var adultCount = tc.AdultCount ?? tc.Count; // legacy null-axis falls back to all-adults pricing
            var childCount = tc.ChildCount ?? 0;

            var adultLineResult = tier.AdultPrice.Multiply(adultCount);
            if (adultLineResult.IsFailure)
                return Result<Money>.Failure(adultLineResult.Errors);

            var childUnitPrice = tier.CalculatePriceForAttendee(Enums.AgeCategory.Child);
            var childLineResult = childUnitPrice.Multiply(childCount);
            if (childLineResult.IsFailure)
                return Result<Money>.Failure(childLineResult.Errors);

            var lineTotalResult = adultLineResult.Value.Add(childLineResult.Value);
            if (lineTotalResult.IsFailure)
                return Result<Money>.Failure(lineTotalResult.Errors);

            if (total == null)
            {
                total = lineTotalResult.Value;
            }
            else
            {
                var addResult = total.Add(lineTotalResult.Value);
                if (addResult.IsFailure)
                    return Result<Money>.Failure(addResult.Errors);
                total = addResult.Value;
            }
        }

        return Result<Money>.Success(total!);
    }
}
