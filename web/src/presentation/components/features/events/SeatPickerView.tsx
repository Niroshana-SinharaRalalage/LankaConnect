/**
 * Slice 7 Chunk S7.4: SeatPickerView — stateful container around the Konva
 * SeatPicker that owns the session / hold / release / timer lifecycle.
 *
 * Designed as a drop-in replacement for [SeatSelector.tsx](./SeatSelector.tsx)
 * with the same input/output contract so Slice 7 Chunk S7.6 can swap the
 * import site in EventRegistrationForm.tsx without touching call-site code.
 *
 *   Props match SeatSelector exactly:
 *     eventId, maxSeats, userId, onSeatsConfirmed(seatIds, sessionId), onCancel
 *
 * The container owns: selection set, 10-minute hold timer, session id, and
 * the hold/release mutations. The renderer (SeatPicker) stays pure — it
 * only turns data into pixels + clicks.
 *
 * Tier-filter wiring: when the layout surfaces `ticketTierIds` on zones +
 * tables and an attendee's tier is known, the container computes an
 * eligibleSeatIds set and passes it to the renderer so ineligible seats
 * render grayed + non-clickable. S7.4 leaves the tier-filter OFF by
 * default (`tierId` prop is optional) because the registration form's
 * attendee-to-tier mapping is still in the SeatSelector-compatible
 * "pick N seats" model. S7.6 evaluates whether to flip on the filter as
 * part of the swap.
 */

'use client';

import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import toast from 'react-hot-toast';
import { Armchair, Clock, Info } from 'lucide-react';

import {
  useHoldSeats,
  useReleaseSeats,
  useSeatAvailability,
  useVenueLayoutByEvent,
} from '@/presentation/hooks/useVenueLayouts';
import { Button } from '@/presentation/components/ui/Button';
import { SeatPicker } from './SeatPicker';
import type {
  HoldSeatsResult,
  SeatAvailabilityDto,
} from '@/infrastructure/api/types/events.types';

export interface SeatPickerViewProps {
  eventId: string;
  maxSeats: number;
  userId: string;
  onSeatsConfirmed: (seatIds: string[], sessionId: string) => void;
  onCancel?: () => void;
  /**
   * Optional: if supplied, seats whose parent zone/table is NOT mapped to
   * this tier via tier_assignments render grayed + non-clickable. When
   * omitted, every Available seat is selectable.
   */
  tierId?: string | null;
}

function pad2(n: number): string {
  return n.toString().padStart(2, '0');
}

function secondsUntil(isoExpiry: string): number {
  const diff = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(0, Math.floor(diff / 1000));
}

export function SeatPickerView({
  eventId,
  maxSeats,
  userId,
  onSeatsConfirmed,
  onCancel,
  tierId,
}: SeatPickerViewProps) {
  // Tell TypeScript userId is reserved for future per-attendee wiring — used
  // by the enclosing registration form today, but routed into seat holds in
  // a later chunk once the backend accepts per-hold user attribution.
  void userId;

  // ── Session + selection state ──────────────────────────────────────
  const sessionIdRef = useRef<string>(crypto.randomUUID());
  const [selectedSeatIds, setSelectedSeatIds] = useState<Set<string>>(new Set());

  // ── Timer state ────────────────────────────────────────────────────
  const [expiresAt, setExpiresAt] = useState<string | null>(null);
  const [secondsLeft, setSecondsLeft] = useState(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  const releasedRef = useRef(false);

  // ── API plumbing ───────────────────────────────────────────────────
  const layoutQuery = useVenueLayoutByEvent(eventId);
  const availabilityQuery = useSeatAvailability(eventId, true /* poll */);
  const holdMutation = useHoldSeats(eventId);
  const releaseMutation = useReleaseSeats(eventId);

  // ── Derived: eligible seat ids for the tier filter ─────────────────
  const eligibleSeatIds = useMemo<Set<string> | undefined>(() => {
    if (!tierId) return undefined;
    const layout = layoutQuery.data;
    if (!layout) return new Set();
    const eligible = new Set<string>();
    for (const zone of layout.zones ?? []) {
      if ((zone.ticketTierIds ?? []).includes(tierId)) {
        for (const s of zone.seats) eligible.add(s.id);
      }
    }
    for (const table of layout.tables ?? []) {
      if ((table.ticketTierIds ?? []).includes(tierId)) {
        for (const s of table.seats) eligible.add(s.id);
      }
    }
    return eligible;
  }, [tierId, layoutQuery.data]);

  // ── Timer helpers ──────────────────────────────────────────────────
  const startTimer = useCallback((expiry: string) => {
    setExpiresAt(expiry);
    setSecondsLeft(secondsUntil(expiry));
    if (timerRef.current) clearInterval(timerRef.current);
    timerRef.current = setInterval(() => {
      const remaining = secondsUntil(expiry);
      setSecondsLeft(remaining);
      if (remaining <= 0 && timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
    }, 1000);
  }, []);

  const stopTimer = useCallback(() => {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    setExpiresAt(null);
    setSecondsLeft(0);
  }, []);

  // ── Hold + release ─────────────────────────────────────────────────
  const releaseHolds = useCallback(() => {
    if (releasedRef.current) return;
    releasedRef.current = true;
    releaseMutation.mutate({ sessionId: sessionIdRef.current });
  }, [releaseMutation]);

  const holdSeats = useCallback(
    (seatIds: string[]) => {
      holdMutation.mutate(
        { sessionId: sessionIdRef.current, seatIds },
        {
          onSuccess: (result: HoldSeatsResult) => {
            startTimer(result.expiresAt);
            releasedRef.current = false;
          },
          onError: (error) => {
            // eslint-disable-next-line no-console
            console.error('[SeatPickerView] hold failed:', error);
            toast.error(
              (error as Error).message ||
                'Failed to hold seats. They may have been taken by someone else.',
            );
            setSelectedSeatIds(new Set());
            stopTimer();
          },
        },
      );
    },
    [holdMutation, startTimer, stopTimer],
  );

  // Expire handler
  useEffect(() => {
    if (expiresAt && secondsLeft <= 0 && selectedSeatIds.size > 0) {
      toast.error('Your seat hold has expired. Please select again.');
      releaseHolds();
      setSelectedSeatIds(new Set());
      stopTimer();
      onCancel?.();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [secondsLeft, expiresAt]);

  // Unmount cleanup
  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
      if (!releasedRef.current && selectedSeatIds.size > 0) {
        releasedRef.current = true;
        releaseMutation.mutate({ sessionId: sessionIdRef.current });
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── Seat click handler ─────────────────────────────────────────────
  const handleSeatClick = useCallback(
    (seatId: string) => {
      setSelectedSeatIds((prev) => {
        const next = new Set(prev);
        if (next.has(seatId)) {
          next.delete(seatId);
        } else if (next.size < maxSeats) {
          next.add(seatId);
        } else {
          toast.error(
            `You can only select up to ${maxSeats} seat${maxSeats !== 1 ? 's' : ''}.`,
          );
          return prev;
        }
        const allSeatIds = Array.from(next);
        if (allSeatIds.length > 0) {
          holdSeats(allSeatIds);
        } else {
          releaseHolds();
          stopTimer();
        }
        return next;
      });
    },
    [maxSeats, holdSeats, releaseHolds, stopTimer],
  );

  // ── Confirm / Cancel ───────────────────────────────────────────────
  const handleConfirm = useCallback(() => {
    const seatIds = Array.from(selectedSeatIds);
    if (seatIds.length !== maxSeats) return;
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
    onSeatsConfirmed(seatIds, sessionIdRef.current);
  }, [selectedSeatIds, maxSeats, onSeatsConfirmed]);

  const handleCancel = useCallback(() => {
    releaseHolds();
    setSelectedSeatIds(new Set());
    stopTimer();
    onCancel?.();
  }, [releaseHolds, stopTimer, onCancel]);

  // ── Derived UI state ───────────────────────────────────────────────
  const isLoading =
    layoutQuery.isLoading || (availabilityQuery.isLoading && !availabilityQuery.data);
  const layout = layoutQuery.data;
  const availability = availabilityQuery.data;
  const timerMinutes = Math.floor(secondsLeft / 60);
  const timerSeconds = secondsLeft % 60;
  const timerUrgent = expiresAt !== null && secondsLeft > 0 && secondsLeft < 120;
  const timerActive = expiresAt !== null && secondsLeft > 0;
  const selectedLabels = useMemo(() => {
    if (!availability) return [];
    return availability
      .filter((s) => selectedSeatIds.has(s.id))
      .sort((a, b) => a.label.localeCompare(b.label, undefined, { numeric: true }))
      .map((s: SeatAvailabilityDto) => s.label);
  }, [availability, selectedSeatIds]);

  // ── Render ─────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div
        className="flex flex-col items-center justify-center py-12 text-neutral-500"
        data-testid="seat-picker-view-loading"
      >
        <Armchair className="w-8 h-8 mb-2 animate-pulse" />
        <p className="text-sm">Loading seat map…</p>
      </div>
    );
  }

  if (!layout) {
    return (
      <div
        className="flex flex-col items-center justify-center py-12 text-neutral-500"
        data-testid="seat-picker-view-no-layout"
      >
        <Info className="w-8 h-8 mb-2" />
        <p className="text-sm">This event doesn&apos;t have a seating layout yet.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4" data-testid="seat-picker-view">
      {/* Timer + instructions */}
      <div className="flex items-start justify-between gap-4">
        <p className="text-sm text-neutral-600">
          Tap seats on the map to select up to{' '}
          <span className="font-medium text-neutral-900">{maxSeats}</span>. Selected
          seats are held for 10 minutes while you finish checkout.
        </p>
        {timerActive && (
          <div
            className={[
              'inline-flex items-center gap-2 rounded-full px-3 py-1 text-sm font-medium',
              timerUrgent ? 'bg-red-50 text-red-700' : 'bg-amber-50 text-amber-700',
            ].join(' ')}
            data-testid="seat-picker-timer"
            aria-live="polite"
          >
            <Clock className="w-4 h-4" aria-hidden="true" />
            {pad2(timerMinutes)}:{pad2(timerSeconds)}
          </div>
        )}
      </div>

      {/* Seat map */}
      <div className="rounded-md border border-neutral-200 bg-white p-2">
        <SeatPicker
          layout={layout}
          availability={availability}
          eligibleSeatIds={eligibleSeatIds}
          selectedSeatIds={selectedSeatIds}
          onSeatClick={handleSeatClick}
        />
      </div>

      {/* Selection summary + actions */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
        <div className="text-sm text-neutral-700" data-testid="seat-picker-summary">
          {selectedSeatIds.size === 0 ? (
            <span className="text-neutral-500">No seats selected yet.</span>
          ) : (
            <>
              <span className="font-medium text-neutral-900">
                {selectedSeatIds.size} / {maxSeats}
              </span>{' '}
              selected — {selectedLabels.join(', ')}
            </>
          )}
        </div>
        <div className="flex items-center gap-2">
          {onCancel && (
            <Button type="button" variant="outline" onClick={handleCancel}>
              Cancel
            </Button>
          )}
          <Button
            type="button"
            onClick={handleConfirm}
            disabled={selectedSeatIds.size !== maxSeats}
            data-testid="seat-picker-confirm"
          >
            Confirm {selectedSeatIds.size > 0 ? `(${selectedSeatIds.size})` : ''}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default SeatPickerView;
