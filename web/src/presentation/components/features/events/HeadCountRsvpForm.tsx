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
  // Phase 7F-C: optional per-tier-by-age split. Only relevant for B2/B4 + tiered events
  // and only on tiers that have a configured ChildPrice (`tier.hasChildPricing === true`).
  // Architect Q2 default: age-unaware default + opt-in toggle per tier.
  const [tierAgeSplit, setTierAgeSplit] = useState<
    Record<string, { adultCount: number; childCount: number } | null>
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
      // For B2/B4 + tiered, demographics must align with the tier total to avoid
      // ambiguous reporting. (Pricing is per tier; demographics are reporting-only.)
      const demoSum =
        registrationMode === RegistrationMode.HeadCountByAge
          ? adults + children
          : registrationMode === RegistrationMode.HeadCountByAgeAndGender
            ? adultMales + adultFemales + childMales + childFemales
            : tierTotal; // B1/B3 don't capture demographics under tiered → no mismatch possible.
      if (demoSum !== tierTotal) {
        setSubmitError(
          `Demographic total (${demoSum}) must match tier total (${tierTotal}). ` +
            'Demographics are for organiser reporting only — pricing is per tier.'
        );
        return;
      }

      // Phase 7F-C: when per-tier age splits are opted-in, the cross-axis sum must agree
      // with the demographic axis (architect Q1 strict) — sum of tier-adultCount must equal
      // demographic adults; same for children. Mirrors the domain-level invariant in
      // HeadCountBreakdown.ValidateTierCounts.
      const anyTierAgeSplit = tierList.some((t) => tierAgeSplit[t.id] != null);
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

    // Build the head-count payload matching backend HeadCountDto.
    const headCount: RsvpRequest['headCount'] = {
      ...(registrationMode === RegistrationMode.HeadCountOnly && {
        total: isTieredEvent ? tierTotal : total,
      }),
      ...(registrationMode === RegistrationMode.HeadCountByAge && { adults, children }),
      ...(registrationMode === RegistrationMode.HeadCountByGender && { males, females }),
      ...(registrationMode === RegistrationMode.HeadCountByAgeAndGender && {
        adultMales,
        adultFemales,
        childMales,
        childFemales,
      }),
      ...(isTieredEvent && {
        tierCounts: Object.entries(tierCounts)
          .filter(([, count]) => count > 0)
          .map(([tierId, count]) => {
            const split = tierAgeSplit[tierId];
            // Phase 7F-C: forward the optional per-tier-by-age axis when the user opted in.
            // Domain factory will reject half-set / sum-mismatch / B1+age-axis combinations.
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

                  {/* Phase 7F-C: opt-in age toggle (only shown when the tier has ChildPrice
                      AND the event is in B2/B4 with tiered pricing). */}
                  {canShowAgeToggle && tierCount > 0 && (
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

                  {/* Helper for tiers without ChildPrice when in B2/B4 — explains why no toggle. */}
                  {ageAxisAvailable && !tier.hasChildPricing && tierCount > 0 && (
                    <p className="text-xs italic text-neutral-500 border-t border-orange-100 pt-2">
                      This tier doesn&rsquo;t have child pricing — children are billed at adult price.
                    </p>
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

      {/* Mode-specific demographic spinners */}
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

        {registrationMode === RegistrationMode.HeadCountByAgeAndGender && (
          <div className="grid grid-cols-2 gap-3">
            <Spinner id="adultMales" label="Adult Males" value={adultMales} onChange={setAdultMales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
            <Spinner id="adultFemales" label="Adult Females" value={adultFemales} onChange={setAdultFemales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
            <Spinner id="childMales" label="Child Males" value={childMales} onChange={setChildMales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
            <Spinner id="childFemales" label="Child Females" value={childFemales} onChange={setChildFemales} min={0} max={maxAttendeesPerRegistration} disabled={isProcessing} />
          </div>
        )}

        {/* Phase 7E.3c (architect edit #3): demographics-for-reporting helper text on tiered events */}
        {isTieredEvent && (registrationMode === RegistrationMode.HeadCountByAge || registrationMode === RegistrationMode.HeadCountByAgeAndGender) && (
          <p className="text-xs italic text-neutral-500">
            Demographics are for organiser reporting only — pricing is per tier.
          </p>
        )}

        {/* For tiered B1/B3 mode hide the entire demographic block (mode doesn't capture them) */}
        {isTieredEvent && (registrationMode === RegistrationMode.HeadCountOnly || registrationMode === RegistrationMode.HeadCountByGender) && (
          <p className="text-xs italic text-neutral-500">
            No demographic capture in this mode — pricing is per tier.
          </p>
        )}

        <p className="text-sm text-neutral-700 border-t border-orange-200 pt-2">
          Total: <strong>{derivedTotal}</strong> attendee{derivedTotal === 1 ? '' : 's'}
        </p>
      </div>

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
