'use client';

import { useEffect, useState } from 'react';
import { AlertTriangle, Loader2, Plus, Minus } from 'lucide-react';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/presentation/components/ui/Dialog';
import { Button } from '@/presentation/components/ui/Button';
import { Input } from '@/presentation/components/ui/Input';
import { eventsRepository } from '@/infrastructure/api/repositories/events.repository';
import {
  RegistrationMode,
  type HeadCountDto,
} from '@/infrastructure/api/types/events.types';

/**
 * Phase 7F-D.4 (architect-approved 2026-04-30): Mode-B counterpart to
 * <see cref="AddAttendeesModal"/>. Renders mode-aware spinners (B1/B2/B3/B4) for the
 * head-count delta the registrant wants to add, then either:
 *   - Free event: backend merges immediately (response.checkoutSessionId === "free-no-stripe").
 *     We close the modal and call onSuccess — no Stripe redirect.
 *   - Paid event: backend returns a Stripe checkout URL; we redirect via window.location.
 *
 * Architect §2.5: free Mode-B uses the same code path as free Mode-A — no fork. The
 * server discriminates based on AdditionalAmount === 0.
 */

interface AddHeadCountModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  registrationId: string;
  mode: RegistrationMode;
  /** Used for the max-attendees cap UI hint. */
  maxAttendeesPerRegistration: number;
  currentAttendeeCount: number;
  /** Called after a successful free-event merge. Paid events redirect to Stripe instead. */
  onSuccess?: () => void;
}

export function AddHeadCountModal({
  open,
  onOpenChange,
  registrationId,
  mode,
  maxAttendeesPerRegistration,
  currentAttendeeCount,
  onSuccess,
}: AddHeadCountModalProps) {
  // Mode-specific delta state.
  const [total, setTotal] = useState(1);
  const [adults, setAdults] = useState(1);
  const [children, setChildren] = useState(0);
  const [males, setMales] = useState(1);
  const [females, setFemales] = useState(0);
  const [adultMales, setAdultMales] = useState(1);
  const [adultFemales, setAdultFemales] = useState(0);
  const [childMales, setChildMales] = useState(0);
  const [childFemales, setChildFemales] = useState(0);

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Reset on close.
  useEffect(() => {
    if (!open) {
      setTotal(1); setAdults(1); setChildren(0);
      setMales(1); setFemales(0);
      setAdultMales(1); setAdultFemales(0); setChildMales(0); setChildFemales(0);
      setError(null);
    }
  }, [open]);

  const deltaTotal =
    mode === RegistrationMode.HeadCountOnly ? total
      : mode === RegistrationMode.HeadCountByAge ? adults + children
        : mode === RegistrationMode.HeadCountByGender ? males + females
          : mode === RegistrationMode.HeadCountByAgeAndGender
            ? adultMales + adultFemales + childMales + childFemales
            : 0;

  const remaining = Math.max(0, maxAttendeesPerRegistration - currentAttendeeCount);
  const overCap = deltaTotal > remaining;

  const handleSubmit = async () => {
    setError(null);
    if (deltaTotal < 1) {
      setError('Add at least one attendee.');
      return;
    }
    if (overCap) {
      setError(`Adding ${deltaTotal} would exceed the per-registration cap of ${maxAttendeesPerRegistration}.`);
      return;
    }

    const delta: HeadCountDto = {
      ...(mode === RegistrationMode.HeadCountOnly && { total }),
      ...(mode === RegistrationMode.HeadCountByAge && { adults, children }),
      ...(mode === RegistrationMode.HeadCountByGender && { males, females }),
      ...(mode === RegistrationMode.HeadCountByAgeAndGender && {
        adultMales, adultFemales, childMales, childFemales,
      }),
    };

    setIsSubmitting(true);
    try {
      const baseUrl = window.location.origin;
      const result = await eventsRepository.initiateAddHeadCount(registrationId, {
        headCountDelta: delta,
        successUrl: `${baseUrl}/events/${registrationId}?addedHeadCount=true`,
        cancelUrl: `${baseUrl}/events/${registrationId}`,
      });

      if (!result.success) {
        setError(result.errorMessage ?? 'Failed to initiate add-headcount.');
        return;
      }

      // Free path: server merges immediately. Paid path: redirect to Stripe.
      if (result.checkoutUrl && result.checkoutSessionId !== 'free-no-stripe') {
        window.location.href = result.checkoutUrl;
        return;
      }

      onSuccess?.();
      onOpenChange(false);
    } catch (err: unknown) {
      const e = err as { response?: { data?: { detail?: string } }; message?: string };
      setError(e?.response?.data?.detail ?? e?.message ?? 'Unexpected error.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Add more attendees</DialogTitle>
          <DialogDescription>
            Add to your existing head-count registration. Free events merge immediately;
            paid events open Stripe checkout for the additional amount.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          <div className="text-sm text-neutral-600">
            Current: <strong>{currentAttendeeCount}</strong>
            {' · '}
            Remaining slots: <strong>{remaining}</strong>
          </div>

          <div className="rounded-lg border border-orange-200 bg-orange-50 p-3 space-y-3">
            {mode === RegistrationMode.HeadCountOnly && (
              <Spinner id="total" label="Add" value={total} onChange={setTotal} min={1} max={remaining} />
            )}
            {mode === RegistrationMode.HeadCountByAge && (
              <div className="grid grid-cols-2 gap-3">
                <Spinner id="adults" label="+ Adults" value={adults} onChange={setAdults} min={0} max={remaining} />
                <Spinner id="children" label="+ Children" value={children} onChange={setChildren} min={0} max={remaining} />
              </div>
            )}
            {mode === RegistrationMode.HeadCountByGender && (
              <div className="grid grid-cols-2 gap-3">
                <Spinner id="males" label="+ Males" value={males} onChange={setMales} min={0} max={remaining} />
                <Spinner id="females" label="+ Females" value={females} onChange={setFemales} min={0} max={remaining} />
              </div>
            )}
            {mode === RegistrationMode.HeadCountByAgeAndGender && (
              <div className="grid grid-cols-2 gap-3">
                <Spinner id="adultMales" label="+ Adult Males" value={adultMales} onChange={setAdultMales} min={0} max={remaining} />
                <Spinner id="adultFemales" label="+ Adult Females" value={adultFemales} onChange={setAdultFemales} min={0} max={remaining} />
                <Spinner id="childMales" label="+ Child Males" value={childMales} onChange={setChildMales} min={0} max={remaining} />
                <Spinner id="childFemales" label="+ Child Females" value={childFemales} onChange={setChildFemales} min={0} max={remaining} />
              </div>
            )}
            <div className="border-t border-orange-200 pt-2 text-sm text-neutral-700">
              Adding: <strong>{deltaTotal}</strong> attendee{deltaTotal === 1 ? '' : 's'}
            </div>
          </div>

          {error && (
            <div className="flex items-start gap-2 rounded-md bg-red-50 border border-red-200 p-3 text-sm text-red-800">
              <AlertTriangle className="h-4 w-4 flex-shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={isSubmitting || deltaTotal < 1 || overCap}
            className="bg-orange-600 hover:bg-orange-700 text-white"
          >
            {isSubmitting ? (
              <><Loader2 className="h-4 w-4 mr-2 animate-spin" /> Processing…</>
            ) : (
              'Continue'
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

interface SpinnerProps {
  id: string;
  label: string;
  value: number;
  onChange: (v: number) => void;
  min: number;
  max: number;
}

function Spinner({ id, label, value, onChange, min, max }: SpinnerProps) {
  return (
    <div>
      <label htmlFor={id} className="block text-xs font-medium text-neutral-700 mb-1">
        {label}
      </label>
      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={() => onChange(Math.max(min, value - 1))}
          disabled={value <= min}
          aria-label={`Decrement ${label}`}
          className="h-9 w-9 rounded-md border border-neutral-300 bg-white text-neutral-700 hover:bg-neutral-50 disabled:opacity-40 disabled:cursor-not-allowed font-medium"
        >
          <Minus className="h-3.5 w-3.5 mx-auto" />
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
          className="w-16 text-center"
        />
        <button
          type="button"
          onClick={() => onChange(Math.min(max, value + 1))}
          disabled={value >= max}
          aria-label={`Increment ${label}`}
          className="h-9 w-9 rounded-md border border-neutral-300 bg-white text-neutral-700 hover:bg-neutral-50 disabled:opacity-40 disabled:cursor-not-allowed font-medium"
        >
          <Plus className="h-3.5 w-3.5 mx-auto" />
        </button>
      </div>
    </div>
  );
}
