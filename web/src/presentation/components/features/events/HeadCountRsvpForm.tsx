'use client';

import { useState, useEffect, type FormEvent } from 'react';
import { Users, AlertCircle } from 'lucide-react';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { useAuthStore } from '@/presentation/store/useAuthStore';
import { useProfileStore } from '@/presentation/store/useProfileStore';
import { RegistrationMode } from '@/infrastructure/api/types/events.types';
import type {
  AnonymousRegistrationRequest,
  RsvpRequest,
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
}: HeadCountRsvpFormProps) {
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

  // Compute the derived total (auto-derived for B2/B3/B4) so the user sees what they're committing.
  const derivedTotal =
    registrationMode === RegistrationMode.HeadCountOnly
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

    // Build the head-count payload matching backend HeadCountDto.
    const headCount: RsvpRequest['headCount'] = {
      ...(registrationMode === RegistrationMode.HeadCountOnly && { total }),
      ...(registrationMode === RegistrationMode.HeadCountByAge && { adults, children }),
      ...(registrationMode === RegistrationMode.HeadCountByGender && { males, females }),
      ...(registrationMode === RegistrationMode.HeadCountByAgeAndGender && {
        adultMales,
        adultFemales,
        childMales,
        childFemales,
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

      {/* Mode-specific demographic spinners */}
      <div className="rounded-lg border border-orange-200 bg-orange-50 p-4 space-y-3">
        <p className="text-sm font-semibold text-neutral-800">
          How many people are you bringing (including yourself)?
        </p>

        {registrationMode === RegistrationMode.HeadCountOnly && (
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

        {registrationMode === RegistrationMode.HeadCountByGender && (
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
