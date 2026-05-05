'use client';

import { useState, useEffect, type FormEvent } from 'react';
import { Users, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { RegistrationMode, TicketingMode } from '@/infrastructure/api/types/events.types';
import type {
  AnonymousRegistrationRequest,
  RsvpRequest,
  TicketTierDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7E.6: Head-count RSVP form for Mode B events (B1 / B2 / B3 / B4).
 *
 * Renders a single Lead-Attendee-Name input + mode-specific demographic spinners,
 * instead of the per-attendee table that <c>EventRegistrationForm</c> shows for Mode A.
 * The submit payload conforms to the same `RsvpRequest` / `AnonymousRegistrationRequest`
 * shape that 7E.3a's controller DTOs expect (LeadAttendeeName + HeadCount).
 *
 * Scope discipline (7E.6, free-only):
 * - Free events ONLY. Paid B-mode (Stripe checkout) is 7E.3b — the form short-circuits
 *   with an "available soon" notice if `isFree=false`.
 * - Tier counts (TierCounts axis) deferred to 7E.3c; tier selector not rendered here.
 * - Donations / sponsors / add-ons / collections deliberately NOT bundled into this
 *   form — Mode B users contribute via the standalone endpoints (per architect §5).
 *
 * Visual style mirrors `EventRegistrationForm` cards (rounded, neutral, compact spinners)
 * for consistent UX across modes.
 */

interface HeadCountRsvpFormProps {
  eventId: string;
  registrationMode: RegistrationMode;
  isFree: boolean;
  maxAttendeesPerRegistration: number;
  spotsLeft: number;
  isProcessing: boolean;
  /** Submit handler — accepts either `RsvpRequest` (auth) or `AnonymousRegistrationRequest`. */
  onSubmit: (data: RsvpRequest | AnonymousRegistrationRequest) => Promise<void>;
  error?: string | null;
  /** Phase 7E.3c: when the event is `Tiered`, render a per-tier counter section. */
  ticketingMode?: TicketingMode;
  /** Phase 7E.3c: tier list for the event — only consumed when `ticketingMode === 'Tiered'`. */
  ticketTiers?: readonly TicketTierDto[];
}

export function HeadCountRsvpForm({
  eventId,
  registrationMode,
  isFree,
  maxAttendeesPerRegistration,
  spotsLeft,
  isProcessing,
  onSubmit,
  error,
  ticketingMode,
  ticketTiers,
}: HeadCountRsvpFormProps) {
  const isTieredEvent = ticketingMode === TicketingMode.Tiered && (ticketTiers?.length ?? 0) > 0;
  const tierList = isTieredEvent ? ticketTiers! : [];
  const { user } = useAuthStore();
  const { profile, loadProfile } = useProfileStore();
  const isAuthenticated = !!user;

  // Form state — controlled inputs; lightweight (no react-hook-form needed for this scope).
  const [leadAttendeeName, setLeadAttendeeName] = useState(
    isAuthenticated ? user.fullName ?? '' : ''
  );
  const [email, setEmail] = useState(isAuthenticated ? user.email ?? '' : '');
  const [phoneNumber, setPhoneNumber] = useState('');

  // Phase 7E.6 fix: phone is REQUIRED at the domain layer (RegistrationContact.Create
  // enforces it for both Mode A and Mode B). Earlier label said "(optional)" — that was
  // wrong and surfaced as a confusing backend rejection. Pre-fill phone from the user's
  // profile when available so most authenticated users don't have to type it again.
  useEffect(() => {
    if (user?.userId && !profile) {
      loadProfile(user.userId).catch(() => {
        // Profile fetch failure is non-blocking — user can still type phone manually.
      });
    }
  }, [user?.userId, profile, loadProfile]);

  useEffect(() => {
    if (isAuthenticated && profile?.phoneNumber && !phoneNumber) {
      setPhoneNumber(profile.phoneNumber);
    }
    // Intentionally narrow deps — only auto-fill once when profile arrives + field is still empty.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated, profile?.phoneNumber]);

  // Mode-specific demographic spinners. All start at 0; submit-time validation enforces
  // the mode's invariants (Total > 0 etc.).
  const [total, setTotal] = useState(1); // B1
  const [adults, setAdults] = useState(1); // B2 / B4 (legacy)
  const [children, setChildren] = useState(0);
  const [males, setMales] = useState(1); // B3
  const [females, setFemales] = useState(0);
  const [adultMales, setAdultMales] = useState(1); // B4
  const [adultFemales, setAdultFemales] = useState(0);
  const [childMales, setChildMales] = useState(0);
  const [childFemales, setChildFemales] = useState(0);

  const [submitError, setSubmitError] = useState<string | null>(null);

  // Phase 7E.3c: per-tier counts state. Map of `tierId → count`. Only used when the event
  // is in `TicketingMode.Tiered`. Initialised lazily from the tier list so the keys exist
  // even if the user never touches a particular tier (count defaults to 0).
  const [tierCounts, setTierCounts] = useState<Record<string, number>>({});
  // Phase 7F-C: per-tier-by-age split. Only relevant for B2/B4 + tiered events on tiers
  // with `hasChildPricing === true`. Phase 7F-E.4b (architect-approved 2026-05-01): the
  // opt-in toggle is REMOVED — when the merged layout applies, the spinners are always
  // visible inline under the tier card. The wire format still uses the same TierCountDto
  // adultCount/childCount fields (Phase 7F-C).
  const [tierAgeSplit, setTierAgeSplit] = useState<
    Record<string, { adultCount: number; childCount: number } | null>
  >({});
  // Phase 7F-E.4b: per-tier gender split (B3 + tiered). UI-only — aggregated to top-level
  // males/females on submit (gender is capture-only, no per-tier pricing axis).
  const [tierGenderSplit, setTierGenderSplit] = useState<
    Record<string, { males: number; females: number }>
  >({});
  // Phase 7F-E.4b: per-tier 4-leaf split (B4 + tiered). UI-only — aggregated to top-level
  // adultMales/adultFemales/childMales/childFemales on submit.
  const [tierFourLeaf, setTierFourLeaf] = useState<
    Record<string, { adultMales: number; adultFemales: number; childMales: number; childFemales: number }>
  >({});
  useEffect(() => {
    if (isTieredEvent && Object.keys(tierCounts).length === 0) {
      const initial: Record<string, number> = {};
      tierList.forEach((t) => {
        initial[t.id] = 0;
      });
      setTierCounts(initial);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isTieredEvent, tierList.length]);

  const tierTotal = Object.values(tierCounts).reduce((sum, n) => sum + n, 0);

  // Phase 7F-C: per-tier-by-age axis is only meaningful in B2 / B4 modes; B1 / B3 capture
  // no age axis at the registration level so the toggle stays hidden in those modes.
  const ageAxisAvailable =
    isTieredEvent &&
    (registrationMode === RegistrationMode.HeadCountByAge ||
      registrationMode === RegistrationMode.HeadCountByAgeAndGender);

  // Phase 7F-E.4b (architect Q4 auto-detect rules): when does the form switch to the
  // merged per-tier × demographic layout?
  //
  //   B2 + tiered + at least one tier with ChildPrice  →  merge age (per-tier Adults / Children)
  //   B3 + tiered                                       →  merge gender (per-tier Males / Females)
  //   B4 + tiered                                       →  merge 4-leaf (AM / AF / CM / CF per tier)
  //
  // When merged, the top-level demographic section is HIDDEN and the registration-level
  // demographic values are derived from the per-tier inputs at submit time.
  const anyTierHasChildPricing = isTieredEvent && tierList.some((t) => t.hasChildPricing);
  const mergeAge =
    isTieredEvent &&
    registrationMode === RegistrationMode.HeadCountByAge &&
    anyTierHasChildPricing;
  const mergeGender =
    isTieredEvent && registrationMode === RegistrationMode.HeadCountByGender;
  const mergeFourLeaf =
    isTieredEvent && registrationMode === RegistrationMode.HeadCountByAgeAndGender;
  const mergedLayout = mergeAge || mergeGender || mergeFourLeaf;

  // Phase 7F-E.4b: under the merged-age layout, the per-tier age split is ALWAYS-ON for
  // tiers that have ChildPrice (no opt-in toggle). Auto-initialise so the spinners are
  // visible the moment the user picks a ticket.
  useEffect(() => {
    if (!mergeAge) return;
    setTierAgeSplit((prev) => {
      let changed = false;
      const next = { ...prev };
      for (const tier of tierList) {
        if (!tier.hasChildPricing) continue;
        const total = tierCounts[tier.id] ?? 0;
        if (total > 0 && next[tier.id] == null) {
          next[tier.id] = { adultCount: total, childCount: 0 };
          changed = true;
        }
      }
      return changed ? next : prev;
    });
  }, [mergeAge, tierList, tierCounts]);

  // Helper: clamp an age leaf so adultCount + childCount === tier total at all times.
  const updateAgeLeaf = (
    tierId: string,
    leaf: 'adultCount' | 'childCount',
    nextValue: number,
  ) => {
    const tierTotalForId = tierCounts[tierId] ?? 0;
    setTierAgeSplit((prev) => {
      const current = prev[tierId] ?? { adultCount: tierTotalForId, childCount: 0 };
      const clamped = Math.max(0, Math.min(tierTotalForId, nextValue));
      const other = leaf === 'adultCount' ? 'childCount' : 'adultCount';
      const next = { ...current, [leaf]: clamped, [other]: tierTotalForId - clamped };
      return { ...prev, [tierId]: next };
    });
  };

  const toggleAgeSplit = (tierId: string, on: boolean) => {
    const tierTotalForId = tierCounts[tierId] ?? 0;
    setTierAgeSplit((prev) => ({
      ...prev,
      [tierId]: on ? { adultCount: tierTotalForId, childCount: 0 } : null,
    }));
  };

  // Phase 7F-E.4b: gender (B3 + tiered) — clamp leaves so males + females === tier total.
  const updateGenderLeaf = (
    tierId: string,
    leaf: 'males' | 'females',
    nextValue: number,
  ) => {
    const tierTotalForId = tierCounts[tierId] ?? 0;
    setTierGenderSplit((prev) => {
      const current = prev[tierId] ?? { males: tierTotalForId, females: 0 };
      const clamped = Math.max(0, Math.min(tierTotalForId, nextValue));
      const other = leaf === 'males' ? 'females' : 'males';
      return { ...prev, [tierId]: { ...current, [leaf]: clamped, [other]: tierTotalForId - clamped } };
    });
  };

  // Phase 7F-E.4b: four-leaf (B4 + tiered) — clamp the chosen leaf, then recompute the
  // remaining counts to keep the sum equal to the tier total. Strategy: drop child leaves
  // first when shrinking, add to adultMales when growing — predictable + matches the
  // age-split convention.
  const updateFourLeaf = (
    tierId: string,
    leaf: 'adultMales' | 'adultFemales' | 'childMales' | 'childFemales',
    nextValue: number,
  ) => {
    const tierTotalForId = tierCounts[tierId] ?? 0;
    setTierFourLeaf((prev) => {
      const current = prev[tierId] ?? {
        adultMales: tierTotalForId,
        adultFemales: 0,
        childMales: 0,
        childFemales: 0,
      };
      const clamped = Math.max(0, Math.min(tierTotalForId, nextValue));
      const next = { ...current, [leaf]: clamped };
      const sum = next.adultMales + next.adultFemales + next.childMales + next.childFemales;
      // If the new value pushes the sum over total, shrink the OTHER leaves in priority order.
      if (sum > tierTotalForId) {
        let overflow = sum - tierTotalForId;
        for (const k of ['childFemales', 'childMales', 'adultFemales', 'adultMales'] as const) {
          if (k === leaf) continue;
          const take = Math.min(overflow, next[k]);
          next[k] = next[k] - take;
          overflow -= take;
          if (overflow <= 0) break;
        }
      } else if (sum < tierTotalForId) {
        // Push remaining attendees onto adultMales by default.
        const slack = tierTotalForId - sum;
        if (leaf !== 'adultMales') next.adultMales += slack;
      }
      return { ...prev, [tierId]: next };
    });
  };

  // Auto-rebalance per-tier B3/B4 splits whenever a tier total changes (mirrors the
  // existing age-split rebalancer above). Lazy init: only materialise once the user
  // picks ≥ 1 ticket on that tier — empty tiers stay empty.
  useEffect(() => {
    if (mergeGender) {
      setTierGenderSplit((prev) => {
        let changed = false;
        const next = { ...prev };
        for (const tier of tierList) {
          const total = tierCounts[tier.id] ?? 0;
          const split = next[tier.id];
          if (!split) {
            if (total > 0) {
              next[tier.id] = { males: total, females: 0 };
              changed = true;
            }
            continue;
          }
          if (split.males + split.females !== total) {
            const males = Math.min(split.males, total);
            next[tier.id] = { males, females: total - males };
            changed = true;
          }
        }
        return changed ? next : prev;
      });
    }
    if (mergeFourLeaf) {
      setTierFourLeaf((prev) => {
        let changed = false;
        const next = { ...prev };
        for (const tier of tierList) {
          const total = tierCounts[tier.id] ?? 0;
          const split = next[tier.id];
          if (!split) {
            if (total > 0) {
              next[tier.id] = { adultMales: total, adultFemales: 0, childMales: 0, childFemales: 0 };
              changed = true;
            }
            continue;
          }
          const sum = split.adultMales + split.adultFemales + split.childMales + split.childFemales;
          if (sum !== total) {
            const withClamp = { ...split };
            let remaining = total;
            for (const k of ['adultMales', 'adultFemales', 'childMales', 'childFemales'] as const) {
              const take = Math.min(remaining, withClamp[k]);
              withClamp[k] = take;
              remaining -= take;
            }
            withClamp.adultMales += remaining;
            next[tier.id] = withClamp;
            changed = true;
          }
        }
        return changed ? next : prev;
      });
    }
  }, [tierCounts, tierList, mergeGender, mergeFourLeaf]);

  // When the user changes the tier total, re-balance the age split so it still sums correctly.
  useEffect(() => {
    setTierAgeSplit((prev) => {
      let changed = false;
      const next = { ...prev };
      for (const tier of tierList) {
        const split = next[tier.id];
        if (!split) continue;
        const total = tierCounts[tier.id] ?? 0;
        if (split.adultCount + split.childCount !== total) {
          // Preserve the proportion as best we can; on shrink, drop children first;
          // on grow, add to adults.
          const adults = Math.min(split.adultCount, total);
          const children = total - adults;
          if (adults !== split.adultCount || children !== split.childCount) {
            next[tier.id] = { adultCount: adults, childCount: children };
            changed = true;
          }
        }
      }
      return changed ? next : prev;
    });
  }, [tierCounts, tierList]);

  // Compute the derived total. For TIERED events the source of truth is the tier counts;
  // demographic spinners (B2/B4) capture organiser-reporting info but don't drive total.
  // For SingleTier events the demographic axes drive total as before.
  const derivedTotal = isTieredEvent
    ? tierTotal
    : registrationMode === RegistrationMode.HeadCountOnly
      ? total
      : registrationMode === RegistrationMode.HeadCountByAge
        ? adults + children
        : registrationMode === RegistrationMode.HeadCountByGender
          ? males + females
          : registrationMode === RegistrationMode.HeadCountByAgeAndGender
            ? adultMales + adultFemales + childMales + childFemales
            : 0;

  const overCapacity = derivedTotal > spotsLeft;
  const overMax = derivedTotal > maxAttendeesPerRegistration;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setSubmitError(null);

    if (!leadAttendeeName.trim()) {
      setSubmitError('Lead attendee name is required.');
      return;
    }

    if (!email.trim()) {
      setSubmitError('Email is required.');
      return;
    }

    if (!phoneNumber.trim()) {
      setSubmitError('Phone number is required.');
      return;
    }

    if (derivedTotal < 1) {
      setSubmitError('At least one attendee is required.');
      return;
    }

    if (overMax) {
      setSubmitError(
        `Maximum ${maxAttendeesPerRegistration} attendees per registration. Requested: ${derivedTotal}.`
      );
      return;
    }

    if (overCapacity) {
      setSubmitError(
        `Only ${spotsLeft} spot${spotsLeft === 1 ? '' : 's'} left. Requested: ${derivedTotal}.`
      );
      return;
    }

    // Phase 7E.3b: paid B-mode is now supported. The backend creates a Stripe Checkout
    // session and the page-level handler redirects to its URL on success (same flow as
    // Mode A's paid path). No client-side short-circuit for paid events.

    // Phase 7E.3c: TIERED events require TierCounts on the head-count payload. For B2/B4
    // the demographic spinners are still captured for organiser reporting, but the TOTAL
    // comes from tierCounts. Validate sum for B2/B4 against the demographic sum.
    if (isTieredEvent) {
      if (tierTotal === 0) {
        setSubmitError('Select at least one ticket from the tier list.');
        return;
      }
      // Phase 7F-E.4b: under the merged layout demographics are derived from per-tier
      // values, so no top-level vs tier-total mismatch can occur. We only validate the
      // top-level demographic equality when the merged layout is OFF.
      if (!mergedLayout) {
        const demoSum =
          registrationMode === RegistrationMode.HeadCountByAge
            ? adults + children
            : registrationMode === RegistrationMode.HeadCountByAgeAndGender
              ? adultMales + adultFemales + childMales + childFemales
              : tierTotal; // B1/B3 (non-merged) don't capture demographics under tiered → no mismatch possible.
        if (demoSum !== tierTotal) {
          setSubmitError(
            `Demographic total (${demoSum}) must match tier total (${tierTotal}). ` +
              'Demographics are for organiser reporting only — pricing is per tier.'
          );
          return;
        }
      }

      // Phase 7F-C: cross-axis sum check between per-tier-age splits and the top-level
      // demographic spinners. Phase 7F-E.4b: the merged layout removes the top-level
      // spinners (the form derives them from per-tier values), so this check no longer
      // applies — skip it. The check still runs for the legacy B4+tiered+ChildPrice opt-in
      // path (mergeFourLeaf is false there because B4 tiered now uses the 4-leaf merge).
      const anyTierAgeSplit = !mergedLayout && tierList.some((t) => tierAgeSplit[t.id] != null);
      if (anyTierAgeSplit) {
        // All-or-nothing across the basket: every tier with non-zero count must opt in.
        const partialSplit = tierList.some(
          (t) => (tierCounts[t.id] ?? 0) > 0 && tierAgeSplit[t.id] == null,
        );
        if (partialSplit) {
          setSubmitError(
            'Per-age split is enabled on some tiers but not others. Enable it on every selected ' +
              'tier (or none) so the age totals can be checked.',
          );
          return;
        }

        const tierAdultSum = tierList.reduce(
          (sum, t) => sum + (tierAgeSplit[t.id]?.adultCount ?? 0),
          0,
        );
        const tierChildSum = tierList.reduce(
          (sum, t) => sum + (tierAgeSplit[t.id]?.childCount ?? 0),
          0,
        );

        const expectedAdults =
          registrationMode === RegistrationMode.HeadCountByAge
            ? adults
            : registrationMode === RegistrationMode.HeadCountByAgeAndGender
              ? adultMales + adultFemales
              : null;
        const expectedChildren =
          registrationMode === RegistrationMode.HeadCountByAge
            ? children
            : registrationMode === RegistrationMode.HeadCountByAgeAndGender
              ? childMales + childFemales
              : null;

        if (expectedAdults != null && tierAdultSum !== expectedAdults) {
          setSubmitError(
            `Per-tier adult total (${tierAdultSum}) must equal demographic adults (${expectedAdults}). ` +
              'Adjust the per-tier split or the Adults spinner so they agree.',
          );
          return;
        }
        if (expectedChildren != null && tierChildSum !== expectedChildren) {
          setSubmitError(
            `Per-tier child total (${tierChildSum}) must equal demographic children (${expectedChildren}). ` +
              'Adjust the per-tier split or the Children spinner so they agree.',
          );
          return;
        }
      }
    }

    // Phase 7F-E.4b: under the merged layout the registration-level demographics are
    // derived from the per-tier inputs (gender / 4-leaf are UI-only; age uses the
    // existing TierCountDto.adultCount/childCount wire fields). Below we compute the
    // values used by the HeadCountDto payload, falling back to top-level state when the
    // merged layout doesn't apply.
    const sumGenderMales = mergeGender
      ? Object.values(tierGenderSplit).reduce((s, x) => s + (x?.males ?? 0), 0)
      : males;
    const sumGenderFemales = mergeGender
      ? Object.values(tierGenderSplit).reduce((s, x) => s + (x?.females ?? 0), 0)
      : females;
    const sumAdultMales = mergeFourLeaf
      ? Object.values(tierFourLeaf).reduce((s, x) => s + (x?.adultMales ?? 0), 0)
      : adultMales;
    const sumAdultFemales = mergeFourLeaf
      ? Object.values(tierFourLeaf).reduce((s, x) => s + (x?.adultFemales ?? 0), 0)
      : adultFemales;
    const sumChildMales = mergeFourLeaf
      ? Object.values(tierFourLeaf).reduce((s, x) => s + (x?.childMales ?? 0), 0)
      : childMales;
    const sumChildFemales = mergeFourLeaf
      ? Object.values(tierFourLeaf).reduce((s, x) => s + (x?.childFemales ?? 0), 0)
      : childFemales;
    const sumAdults = mergeAge
      ? Object.values(tierAgeSplit).reduce((s, x) => s + (x?.adultCount ?? 0), 0)
      : adults;
    const sumChildren = mergeAge
      ? Object.values(tierAgeSplit).reduce((s, x) => s + (x?.childCount ?? 0), 0)
      : children;

    // Build the head-count payload matching backend HeadCountDto.
    const headCount: RsvpRequest['headCount'] = {
      ...(registrationMode === RegistrationMode.HeadCountOnly && {
        total: isTieredEvent ? tierTotal : total,
      }),
      ...(registrationMode === RegistrationMode.HeadCountByAge && {
        adults: sumAdults, children: sumChildren,
      }),
      ...(registrationMode === RegistrationMode.HeadCountByGender && {
        males: sumGenderMales, females: sumGenderFemales,
      }),
      ...(registrationMode === RegistrationMode.HeadCountByAgeAndGender && {
        adultMales: sumAdultMales,
        adultFemales: sumAdultFemales,
        childMales: sumChildMales,
        childFemales: sumChildFemales,
      }),
      ...(isTieredEvent && {
        tierCounts: Object.entries(tierCounts)
          .filter(([, count]) => count > 0)
          .map(([tierId, count]) => {
            const split = tierAgeSplit[tierId];
            const fourLeaf = tierFourLeaf[tierId];
            // Phase 7F-E.7 (architect-approved 2026-05-04, re-opens §2.2 #4): forward
            // the per-tier 4-leaf demographic split when the merged-4-leaf layout is on
            // (B4 + tiered). Domain factory auto-derives age split from the 4-leaf for
            // back-compat with the 7F-C pricing helper, so we don't also have to send
            // adultCount/childCount on this path.
            if (mergeFourLeaf && fourLeaf != null) {
              return {
                tierId,
                count,
                adultMaleCount: fourLeaf.adultMales,
                adultFemaleCount: fourLeaf.adultFemales,
                childMaleCount: fourLeaf.childMales,
                childFemaleCount: fourLeaf.childFemales,
              };
            }
            // Phase 7F-C: forward the optional per-tier-by-age axis when present (always
            // present under 7F-E.4b mergeAge, opt-in under the legacy 7F-C B4 path).
            return split != null
              ? { tierId, count, adultCount: split.adultCount, childCount: split.childCount }
              : { tierId, count };
          }),
      }),
    };

    if (isAuthenticated) {
      const rsvp: RsvpRequest = {
        userId: user.userId,
        leadAttendeeName: leadAttendeeName.trim(),
        headCount,
        email: email.trim(),
        phoneNumber: phoneNumber.trim() || undefined,
      };
      await onSubmit(rsvp);
    } else {
      const anon: AnonymousRegistrationRequest = {
        email: email.trim(),
        phoneNumber: phoneNumber.trim(),
        leadAttendeeName: leadAttendeeName.trim(),
        headCount,
        // Legacy fields the backend tolerates but doesn't use for B mode.
        name: leadAttendeeName.trim(),
        quantity: derivedTotal,
      };
      await onSubmit(anon);
    }
  };

  const showError = submitError ?? error;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <div className="flex items-center gap-2 text-sm text-neutral-600">
        <Users className="h-4 w-4 text-orange-600" />
        <span>
          {derivedTotal} of {spotsLeft} spot{spotsLeft === 1 ? '' : 's'} left
          {overCapacity && (
            <span className="ml-2 text-destructive font-medium">
              (exceeds available spots)
            </span>
          )}
        </span>
      </div>

      <div>
        <label htmlFor="leadAttendeeName" className="block text-sm font-medium text-neutral-700 mb-1">
          Lead Attendee Name <span className="text-destructive">*</span>
        </label>
        <Input
          id="leadAttendeeName"
          type="text"
          required
          value={leadAttendeeName}
          onChange={(e) => setLeadAttendeeName(e.target.value)}
          placeholder="Your full name"
          disabled={isProcessing}
        />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-neutral-700 mb-1">
            Email {!isAuthenticated && <span className="text-destructive">*</span>}
          </label>
          <Input
            id="email"
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
            disabled={isProcessing || isAuthenticated}
            className={isAuthenticated ? 'bg-neutral-50 text-neutral-600 cursor-not-allowed' : undefined}
          />
          {isAuthenticated && (
            <p className="text-xs text-neutral-500 mt-1">Using your account email</p>
          )}
        </div>
        <div>
          <label htmlFor="phoneNumber" className="block text-sm font-medium text-neutral-700 mb-1">
            Phone <span className="text-destructive">*</span>
          </label>
          <Input
            id="phoneNumber"
            type="tel"
            required
            value={phoneNumber}
            onChange={(e) => setPhoneNumber(e.target.value)}
            placeholder="555-0100"
            disabled={isProcessing}
          />
        </div>
      </div>

      {/* Phase 7E.3c — Tier-count selector for tiered events. Phase 7F-C: opt-in
          per-tier-by-age axis on B2/B4 modes when the tier has child pricing configured. */}
      {isTieredEvent && (
        <div className="rounded-lg border border-orange-200 bg-orange-50 p-4 space-y-3">
          <p className="text-sm font-semibold text-neutral-800">
            Choose your ticket tier(s)
          </p>
          <div className="space-y-2">
            {tierList.map((tier) => {
              const tierCount = tierCounts[tier.id] ?? 0;
              const split = tierAgeSplit[tier.id] ?? null;
              const splitOn = split !== null;
              // Architect Q6: hide the toggle entirely when the tier has no ChildPrice.
              const canShowAgeToggle = ageAxisAvailable && tier.hasChildPricing;

              return (
                <div
                  key={tier.id}
                  className="bg-white rounded-md border border-orange-100 px-3 py-2 space-y-2"
                >
                  <div className="flex items-center justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-neutral-900 truncate">{tier.name}</p>
                      <p className="text-xs text-neutral-500">
                        {tier.isFree
                          ? 'Free'
                          : `$${tier.adultPriceAmount.toFixed(2)} ${tier.adultPriceCurrency}`}
                        {tier.hasChildPricing && tier.childPriceAmount != null
                          ? ` · child $${tier.childPriceAmount.toFixed(2)}`
                          : ''}
                        {tier.availableQuantity > 0
                          ? ` · ${tier.availableQuantity} left`
                          : ' · sold out'}
                      </p>
                    </div>
                    <Spinner
                      id={`tier-${tier.id}`}
                      label=""
                      value={tierCount}
                      onChange={(v) => setTierCounts((prev) => ({ ...prev, [tier.id]: v }))}
                      min={0}
                      max={Math.min(tier.availableQuantity, maxAttendeesPerRegistration)}
                      disabled={isProcessing}
                    />
                  </div>

                  {/* Phase 7F-C → 7F-E.4b: when the merged-age layout applies (B2 + tiered
                      + ChildPrice on at least one tier), the per-tier Adults / Children
                      spinners are ALWAYS shown for tiers that support child pricing — no
                      opt-in toggle. Tiers without ChildPrice still surface the original
                      "billed at adult price" helper. */}
                  {mergeAge && tier.hasChildPricing && tierCount > 0 && split && (
                    <div className="flex flex-wrap items-center gap-3 border-t border-orange-100 pt-2">
                      <Spinner
                        id={`tier-${tier.id}-adults`}
                        label="Adults"
                        value={split.adultCount}
                        onChange={(v) => updateAgeLeaf(tier.id, 'adultCount', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                      <Spinner
                        id={`tier-${tier.id}-children`}
                        label="Children"
                        value={split.childCount}
                        onChange={(v) => updateAgeLeaf(tier.id, 'childCount', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                    </div>
                  )}

                  {/* Phase 7F-C compat: keep the OPT-IN toggle for B4 + tiered + ChildPrice
                      mode (B4 doesn't auto-merge age — only gender does). */}
                  {canShowAgeToggle && !mergeAge && tierCount > 0 && (
                    <div className="flex flex-wrap items-center gap-3 border-t border-orange-100 pt-2">
                      <label className="flex items-center gap-2 text-xs text-neutral-700 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={splitOn}
                          onChange={(e) => toggleAgeSplit(tier.id, e.target.checked)}
                          disabled={isProcessing}
                          className="h-3.5 w-3.5"
                        />
                        Add per-age split (adults / children)
                      </label>
                      {splitOn && split && (
                        <div className="flex items-center gap-3">
                          <Spinner
                            id={`tier-${tier.id}-adults`}
                            label="Adults"
                            value={split.adultCount}
                            onChange={(v) => updateAgeLeaf(tier.id, 'adultCount', v)}
                            min={0}
                            max={tierCount}
                            disabled={isProcessing}
                          />
                          <Spinner
                            id={`tier-${tier.id}-children`}
                            label="Children"
                            value={split.childCount}
                            onChange={(v) => updateAgeLeaf(tier.id, 'childCount', v)}
                            min={0}
                            max={tierCount}
                            disabled={isProcessing}
                          />
                        </div>
                      )}
                    </div>
                  )}

                  {/* Helper for tiers without ChildPrice when in B2/B4 — explains why no
                      age spinners surface on this tier. */}
                  {ageAxisAvailable && !tier.hasChildPricing && tierCount > 0 && (
                    <p className="text-xs italic text-neutral-500 border-t border-orange-100 pt-2">
                      This tier doesn&rsquo;t have child pricing — children are billed at adult price.
                    </p>
                  )}

                  {/* Phase 7F-E.4b: B3 + tiered → per-tier Males / Females spinners
                      (always-on; gender is capture-only with no pricing dependency). */}
                  {mergeGender && tierCount > 0 && tierGenderSplit[tier.id] && (
                    <div className="flex flex-wrap items-center gap-3 border-t border-orange-100 pt-2">
                      <Spinner
                        id={`tier-${tier.id}-males`}
                        label="Males"
                        value={tierGenderSplit[tier.id]!.males}
                        onChange={(v) => updateGenderLeaf(tier.id, 'males', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                      <Spinner
                        id={`tier-${tier.id}-females`}
                        label="Females"
                        value={tierGenderSplit[tier.id]!.females}
                        onChange={(v) => updateGenderLeaf(tier.id, 'females', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                    </div>
                  )}

                  {/* Phase 7F-E.4b: B4 + tiered → per-tier 4-leaf spinners (always-on). */}
                  {mergeFourLeaf && tierCount > 0 && tierFourLeaf[tier.id] && (
                    <div className="grid grid-cols-2 gap-3 border-t border-orange-100 pt-2">
                      <Spinner
                        id={`tier-${tier.id}-am`}
                        label="Adult Males"
                        value={tierFourLeaf[tier.id]!.adultMales}
                        onChange={(v) => updateFourLeaf(tier.id, 'adultMales', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                      <Spinner
                        id={`tier-${tier.id}-af`}
                        label="Adult Females"
                        value={tierFourLeaf[tier.id]!.adultFemales}
                        onChange={(v) => updateFourLeaf(tier.id, 'adultFemales', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                      <Spinner
                        id={`tier-${tier.id}-cm`}
                        label="Child Males"
                        value={tierFourLeaf[tier.id]!.childMales}
                        onChange={(v) => updateFourLeaf(tier.id, 'childMales', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                      <Spinner
                        id={`tier-${tier.id}-cf`}
                        label="Child Females"
                        value={tierFourLeaf[tier.id]!.childFemales}
                        onChange={(v) => updateFourLeaf(tier.id, 'childFemales', v)}
                        min={0}
                        max={tierCount}
                        disabled={isProcessing}
                      />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
          <p className="text-sm text-neutral-700 border-t border-orange-200 pt-2">
            Total: <strong>{tierTotal}</strong> attendee{tierTotal === 1 ? '' : 's'}
          </p>
        </div>
      )}

      {/* Phase 7F-E.4b: top-level demographic block is HIDDEN when the merged layout is
          active (per-tier spinners under each tier card take over). It still renders for
          non-tiered B-modes, B1+tiered (no demographics in this mode), and B2+tiered
          without ChildPrice (architect rule: pricing depends on per-tier age, so users
          must capture top-level adults/children when no tier offers child pricing). */}
      {!mergedLayout && (
        <div className="rounded-lg border border-orange-200 bg-orange-50 p-4 space-y-3">
          <p className="text-sm font-semibold text-neutral-800">
            {isTieredEvent
              ? 'Tell us about your group (for organiser reporting)'
              : 'How many people are you bringing (including yourself)?'}
          </p>

          {registrationMode === RegistrationMode.HeadCountOnly && !isTieredEvent && (
            <Spinner
              id="total"
              label="Total attendees"
              value={total}
              onChange={setTotal}
              min={1}
              max={maxAttendeesPerRegistration}
              disabled={isProcessing}
            />
          )}

          {registrationMode === RegistrationMode.HeadCountByAge && (
            <div className="grid grid-cols-2 gap-3">
              <Spinner id="adults" label="Adults" value={adults} onChange={setAdults} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
              <Spinner id="children" label="Children" value={children} onChange={setChildren} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
            </div>
          )}

          {registrationMode === RegistrationMode.HeadCountByGender && !isTieredEvent && (
            <div className="grid grid-cols-2 gap-3">
              <Spinner id="males" label="Males" value={males} onChange={setMales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
              <Spinner id="females" label="Females" value={females} onChange={setFemales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
            </div>
          )}

          {registrationMode === RegistrationMode.HeadCountByAgeAndGender && !isTieredEvent && (
            <div className="grid grid-cols-2 gap-3">
              <Spinner id="adultMales" label="Adult Males" value={adultMales} onChange={setAdultMales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
              <Spinner id="adultFemales" label="Adult Females" value={adultFemales} onChange={setAdultFemales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
              <Spinner id="childMales" label="Child Males" value={childMales} onChange={setChildMales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
              <Spinner id="childFemales" label="Child Females" value={childFemales} onChange={setChildFemales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
            </div>
          )}

          {/* Phase 7E.3c (architect edit #3): demographics-for-reporting helper text on tiered events */}
          {isTieredEvent && registrationMode === RegistrationMode.HeadCountByAge && (
            <p className="text-xs italic text-neutral-500">
              No tier offers child pricing — capture the group breakdown here for organiser reporting.
            </p>
          )}

          {/* For tiered B1 mode hide the entire demographic block (mode doesn't capture demographics) */}
          {isTieredEvent && registrationMode === RegistrationMode.HeadCountOnly && (
            <p className="text-xs italic text-neutral-500">
              No demographic capture in this mode — pricing is per tier.
            </p>
          )}

          <p className="text-sm text-neutral-700 border-t border-orange-200 pt-2">
            Total: <strong>{derivedTotal}</strong> attendee{derivedTotal === 1 ? '' : 's'}
          </p>
        </div>
      )}

      {/* Phase 7F-E.4b: under the merged layout the totals header sits below the tier
          cards (since the per-tier spinners replace the top-level demographic block). */}
      {mergedLayout && (
        <p className="text-sm text-neutral-700">
          Total: <strong>{derivedTotal}</strong> attendee{derivedTotal === 1 ? '' : 's'}{' '}
          <span className="text-xs italic text-neutral-500">
            (derived from your per-tier selection)
          </span>
        </p>
      )}

      {showError && (
        <div className="flex items-start gap-2 rounded-md bg-red-50 border border-red-200 p-3 text-sm text-red-800">
          <AlertCircle className="h-4 w-4 flex-shrink-0 mt-0.5" />
          <span>{showError}</span>
        </div>
      )}

      <Button
        type="submit"
        disabled={isProcessing || overCapacity || overMax || derivedTotal < 1}
        className="w-full"
      >
        {isProcessing ? 'Submitting…' : 'RSVP'}
      </Button>
    </form>
  );
}

/**
 * Compact +/- spinner. Matches the visual style of existing event-form numeric inputs but
 * keeps tap targets touch-friendly on mobile.
 */
interface SpinnerProps {
  id: string;
  label: string;
  value: number;
  onChange: (value: number) => void;
  min: number;
  max: number;
  disabled?: boolean;
}

function Spinner({ id, label, value, onChange, min, max, disabled = false }: SpinnerProps) {
  const decrement = () => onChange(Math.max(min, value - 1));
  const increment = () => onChange(Math.min(max, value + 1));

  return (
    <div>
      <label htmlFor={id} className="block text-xs font-medium text-neutral-700 mb-1">
        {label}
      </label>
      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={decrement}
          disabled={disabled || value <= min}
          className="h-9 w-9 rounded-md border border-neutral-300 bg-white text-neutral-700 hover:bg-neutral-50 disabled:opacity-40 disabled:cursor-not-allowed font-medium"
          aria-label={`Decrement ${label}`}
        >
          −
        </button>
        <Input
          id={id}
          type="number"
          min={min}
          max={max}
          value={value}
          onChange={(e) => {
            const parsed = parseInt(e.target.value, 10);
            if (!Number.isNaN(parsed)) onChange(Math.max(min, Math.min(max, parsed)));
          }}
          disabled={disabled}
          className="w-16 text-center"
        />
        <button
          type="button"
          onClick={increment}
          disabled={disabled || value >= max}
          className="h-9 w-9 rounded-md border border-neutral-300 bg-white text-neutral-700 hover:bg-neutral-50 disabled:opacity-40 disabled:cursor-not-allowed font-medium"
          aria-label={`Increment ${label}`}
        >
          +
        </button>
      </div>
    </div>
  );
}
