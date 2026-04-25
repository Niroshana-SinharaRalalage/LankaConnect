'use client';

import React, { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { Armchair, Clock, X, Check, AlertTriangle, Info } from 'lucide-react';
import toast from 'react-hot-toast';

import { Button } from '@/presentation/components/ui/Button';
import {
  useSeatAvailability,
  useHoldSeats,
  useReleaseSeats,
} from '@/presentation/hooks/useVenueLayouts';
import type {
  SeatAvailabilityDto,
  HoldSeatsResult,
} from '@/infrastructure/api/types/events.types';
import type { SeatStatus } from '@/infrastructure/api/types/events.types';

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

interface SeatSelectorProps {
  eventId: string;
  maxSeats: number;
  onSeatsConfirmed: (seatIds: string[], sessionId: string) => void;
  onCancel?: () => void;
  userId: string;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Pad a number to two digits for MM:SS display. */
function pad2(n: number): string {
  return n.toString().padStart(2, '0');
}

/** Compute seconds remaining from an ISO expiry timestamp. */
function secondsUntil(isoExpiry: string): number {
  const diff = new Date(isoExpiry).getTime() - Date.now();
  return Math.max(0, Math.floor(diff / 1000));
}

/** Group seats by zone, preserving zone order, then by row within each zone. */
function groupByZone(
  seats: SeatAvailabilityDto[]
): Array<{
  zoneId: string;
  zoneName: string;
  zoneColor: string;
  availableCount: number;
  totalCount: number;
  rows: Array<{
    rowLabel: string;
    seats: SeatAvailabilityDto[];
  }>;
}> {
  const zoneMap = new Map<
    string,
    {
      zoneId: string;
      zoneName: string;
      zoneColor: string;
      seats: SeatAvailabilityDto[];
    }
  >();

  for (const seat of seats) {
    let zone = zoneMap.get(seat.zoneId);
    if (!zone) {
      zone = {
        zoneId: seat.zoneId,
        zoneName: seat.zoneName,
        zoneColor: seat.zoneColor,
        seats: [],
      };
      zoneMap.set(seat.zoneId, zone);
    }
    zone.seats.push(seat);
  }

  return Array.from(zoneMap.values()).map((zone) => {
    // Group seats by row within this zone
    const rowMap = new Map<string, SeatAvailabilityDto[]>();
    for (const seat of zone.seats) {
      let row = rowMap.get(seat.row);
      if (!row) {
        row = [];
        rowMap.set(seat.row, row);
      }
      row.push(seat);
    }

    // Sort seats within each row by seat number
    const rows = Array.from(rowMap.entries())
      .sort(([a], [b]) => a.localeCompare(b, undefined, { numeric: true }))
      .map(([rowLabel, rowSeats]) => ({
        rowLabel,
        seats: rowSeats.sort((a, b) => a.number - b.number),
      }));

    const enabledSeats = zone.seats.filter((s) => s.isEnabled);

    return {
      zoneId: zone.zoneId,
      zoneName: zone.zoneName,
      zoneColor: zone.zoneColor,
      availableCount: enabledSeats.filter((s) => s.status === 'Available').length,
      totalCount: enabledSeats.length,
      rows,
    };
  });
}

// ---------------------------------------------------------------------------
// Seat circle component
// ---------------------------------------------------------------------------

interface SeatCircleProps {
  seat: SeatAvailabilityDto;
  isSelected: boolean;
  canSelect: boolean;
  onClick: (seatId: string) => void;
}

function SeatCircle({ seat, isSelected, canSelect, onClick }: SeatCircleProps) {
  const [showTooltip, setShowTooltip] = useState(false);

  // Determine visual state
  const isDisabled = !seat.isEnabled || seat.status === 'Disabled';
  const isReserved = seat.status === 'Reserved';
  const isHeldByOthers = seat.status === 'Held' && !isSelected;
  const isAvailable = seat.status === 'Available';

  const isClickable = isSelected || (isAvailable && canSelect);

  // Build classes for the seat circle
  let bgColor: string;
  let borderStyle: string;
  let cursor: string;
  let opacity: string;

  if (isDisabled) {
    bgColor = 'bg-slate-800';
    borderStyle = 'border-2 border-slate-700';
    cursor = 'cursor-not-allowed';
    opacity = 'opacity-30';
  } else if (isReserved) {
    bgColor = 'bg-slate-400';
    borderStyle = 'border-2 border-slate-500';
    cursor = 'cursor-not-allowed';
    opacity = 'opacity-60';
  } else if (isHeldByOthers) {
    bgColor = 'bg-amber-300';
    borderStyle = 'border-2 border-amber-400';
    cursor = 'cursor-not-allowed';
    opacity = 'opacity-80';
  } else if (isSelected) {
    bgColor = 'bg-blue-500';
    borderStyle = 'border-2 border-blue-300 ring-2 ring-blue-400/50';
    cursor = 'cursor-pointer';
    opacity = '';
  } else if (isAvailable && canSelect) {
    bgColor = '';
    borderStyle = 'border-2 border-current';
    cursor = 'cursor-pointer';
    opacity = '';
  } else {
    // Available but max seats reached
    bgColor = '';
    borderStyle = 'border-2 border-current';
    cursor = 'cursor-not-allowed';
    opacity = 'opacity-40';
  }

  const handleClick = useCallback(() => {
    if (isClickable) {
      onClick(seat.id);
    }
  }, [isClickable, onClick, seat.id]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if ((e.key === 'Enter' || e.key === ' ') && isClickable) {
        e.preventDefault();
        onClick(seat.id);
      }
    },
    [isClickable, onClick, seat.id]
  );

  if (isDisabled) {
    return <div className="w-8 h-8" aria-hidden="true" />;
  }

  return (
    <div className="relative inline-flex items-center justify-center">
      <button
        type="button"
        className={`w-8 h-8 rounded-full flex items-center justify-center text-[10px] font-medium transition-all duration-150 focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-1 focus-visible:ring-blue-500 ${bgColor} ${borderStyle} ${cursor} ${opacity}`}
        style={{
          color: isSelected || isReserved || isHeldByOthers ? undefined : seat.zoneColor,
          borderColor:
            isSelected || isReserved || isHeldByOthers || isDisabled ? undefined : seat.zoneColor,
          backgroundColor:
            !isSelected && !isReserved && !isHeldByOthers && !isDisabled && isAvailable
              ? `${seat.zoneColor}20`
              : undefined,
        }}
        disabled={!isClickable}
        onClick={handleClick}
        onKeyDown={handleKeyDown}
        onMouseEnter={() => setShowTooltip(true)}
        onMouseLeave={() => setShowTooltip(false)}
        onFocus={() => setShowTooltip(true)}
        onBlur={() => setShowTooltip(false)}
        aria-label={`Seat ${seat.label}, ${seat.zoneName}, ${isSelected ? 'selected' : isReserved ? 'reserved' : isHeldByOthers ? 'held by another user' : isAvailable ? 'available' : 'unavailable'}`}
        aria-pressed={isSelected}
        role="switch"
      >
        {seat.number}
      </button>

      {/* Tooltip */}
      {showTooltip && (
        <div
          className="absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 bg-slate-900 text-white text-xs rounded shadow-lg whitespace-nowrap z-50 pointer-events-none"
          role="tooltip"
        >
          <div className="font-medium">{seat.label}</div>
          <div className="text-slate-300">{seat.zoneName}</div>
          {seat.isAccessible && <div className="text-blue-300">Accessible</div>}
          <div className="absolute top-full left-1/2 -translate-x-1/2 -mt-px border-4 border-transparent border-t-slate-900" />
        </div>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main SeatSelector component
// ---------------------------------------------------------------------------

export function SeatSelector({
  eventId,
  maxSeats,
  onSeatsConfirmed,
  onCancel,
  userId,
}: SeatSelectorProps) {
  // Stable session ID for the entire component lifecycle
  const sessionIdRef = useRef<string>(crypto.randomUUID());

  // Selection state
  const [selectedSeatIds, setSelectedSeatIds] = useState<Set<string>>(new Set());

  // Timer state
  const [expiresAt, setExpiresAt] = useState<string | null>(null);
  const [secondsLeft, setSecondsLeft] = useState<number>(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // Track whether holds have been released (to avoid double-release)
  const releasedRef = useRef(false);

  // API hooks
  const { data: seatAvailability, isLoading: isLoadingSeats } = useSeatAvailability(
    eventId,
    true // polling every 5s
  );
  const holdSeatsMutation = useHoldSeats(eventId);
  const releaseSeatsMutation = useReleaseSeats(eventId);

  // Grouped seat data
  const zones = useMemo(() => {
    if (!seatAvailability) return [];
    return groupByZone(seatAvailability);
  }, [seatAvailability]);

  // Get labels for selected seats
  const selectedSeatLabels = useMemo(() => {
    if (!seatAvailability) return [];
    return seatAvailability
      .filter((s) => selectedSeatIds.has(s.id))
      .sort((a, b) => a.label.localeCompare(b.label, undefined, { numeric: true }))
      .map((s) => s.label);
  }, [seatAvailability, selectedSeatIds]);

  // ---------------------------------------------------------------------------
  // Timer management
  // ---------------------------------------------------------------------------

  const startTimer = useCallback((expiry: string) => {
    setExpiresAt(expiry);
    setSecondsLeft(secondsUntil(expiry));

    // Clear any existing timer
    if (timerRef.current) {
      clearInterval(timerRef.current);
    }

    timerRef.current = setInterval(() => {
      const remaining = secondsUntil(expiry);
      setSecondsLeft(remaining);

      if (remaining <= 0) {
        if (timerRef.current) {
          clearInterval(timerRef.current);
          timerRef.current = null;
        }
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

  // Handle timer expiry: release holds and clear selection
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

  // Clean up timer and release holds on unmount
  useEffect(() => {
    return () => {
      if (timerRef.current) {
        clearInterval(timerRef.current);
      }
      // Fire-and-forget release on unmount
      if (!releasedRef.current && selectedSeatIds.size > 0) {
        releasedRef.current = true;
        releaseSeatsMutation.mutate({ sessionId: sessionIdRef.current });
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ---------------------------------------------------------------------------
  // Hold / release helpers
  // ---------------------------------------------------------------------------

  const holdSeats = useCallback(
    (seatIds: string[]) => {
      holdSeatsMutation.mutate(
        {
          sessionId: sessionIdRef.current,
          seatIds,
        },
        {
          onSuccess: (result: HoldSeatsResult) => {
            startTimer(result.expiresAt);
            releasedRef.current = false;
          },
          onError: (error) => {
            toast.error(
              error.message || 'Failed to hold seats. They may have been taken by someone else.'
            );
            // Remove seats that failed to hold from selection
            setSelectedSeatIds(new Set());
            stopTimer();
          },
        }
      );
    },
    [holdSeatsMutation, startTimer, stopTimer]
  );

  const releaseHolds = useCallback(() => {
    if (releasedRef.current) return;
    releasedRef.current = true;
    releaseSeatsMutation.mutate({ sessionId: sessionIdRef.current });
  }, [releaseSeatsMutation]);

  // ---------------------------------------------------------------------------
  // Seat click handler
  // ---------------------------------------------------------------------------

  const handleSeatClick = useCallback(
    (seatId: string) => {
      setSelectedSeatIds((prev) => {
        const next = new Set(prev);

        if (next.has(seatId)) {
          // Deselect: remove from selection but keep holds active
          next.delete(seatId);
        } else if (next.size < maxSeats) {
          // Select: add to selection
          next.add(seatId);
        } else {
          toast.error(`You can only select up to ${maxSeats} seat${maxSeats !== 1 ? 's' : ''}.`);
          return prev;
        }

        // Send hold request with ALL currently selected seats
        // The API replaces the previous session holds
        const allSeatIds = Array.from(next);
        if (allSeatIds.length > 0) {
          holdSeats(allSeatIds);
        } else {
          // No seats selected — release holds
          releaseHolds();
          stopTimer();
        }

        return next;
      });
    },
    [maxSeats, holdSeats, releaseHolds, stopTimer]
  );

  // ---------------------------------------------------------------------------
  // Confirm handler
  // ---------------------------------------------------------------------------

  const handleConfirm = useCallback(() => {
    const seatIds = Array.from(selectedSeatIds);
    if (seatIds.length !== maxSeats) return;

    // Stop the timer — seats are now confirmed, the registration flow will handle them
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }

    onSeatsConfirmed(seatIds, sessionIdRef.current);
  }, [selectedSeatIds, maxSeats, onSeatsConfirmed]);

  // ---------------------------------------------------------------------------
  // Cancel handler
  // ---------------------------------------------------------------------------

  const handleCancel = useCallback(() => {
    releaseHolds();
    setSelectedSeatIds(new Set());
    stopTimer();
    onCancel?.();
  }, [releaseHolds, stopTimer, onCancel]);

  // ---------------------------------------------------------------------------
  // Timer display
  // ---------------------------------------------------------------------------

  const timerMinutes = Math.floor(secondsLeft / 60);
  const timerSeconds = secondsLeft % 60;
  const isTimerUrgent = expiresAt !== null && secondsLeft < 120;
  const isTimerActive = expiresAt !== null && secondsLeft > 0;

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  if (isLoadingSeats && !seatAvailability) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <Armchair className="w-10 h-10 mb-3 animate-pulse" />
        <p className="text-sm">Loading seat map...</p>
      </div>
    );
  }

  if (!seatAvailability || seatAvailability.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-slate-500">
        <AlertTriangle className="w-10 h-10 mb-3" />
        <p className="text-sm">No seats available for this event.</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-5">
      {/* ------------------------------------------------------------------ */}
      {/* Header: title, count, timer                                        */}
      {/* ------------------------------------------------------------------ */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-slate-900">Select Your Seats</h3>
          <p className="text-sm text-slate-500 mt-0.5">
            {selectedSeatIds.size} of {maxSeats} seat{maxSeats !== 1 ? 's' : ''} selected
          </p>
        </div>

        {isTimerActive && (
          <div
            className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-md border text-sm font-medium transition-colors ${
              isTimerUrgent
                ? 'bg-red-50 border-red-200 text-red-700'
                : 'bg-blue-50 border-blue-200 text-blue-700'
            }`}
            role="timer"
            aria-live="polite"
            aria-label={`Hold expires in ${timerMinutes} minutes and ${timerSeconds} seconds`}
          >
            <Clock className={`w-4 h-4 ${isTimerUrgent ? 'text-red-500' : 'text-blue-500'}`} />
            <span>
              Hold expires: {pad2(timerMinutes)}:{pad2(timerSeconds)}
            </span>
          </div>
        )}
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Zone legend with availability counts                               */}
      {/* ------------------------------------------------------------------ */}
      <div className="flex flex-wrap gap-3">
        {zones.map((zone) => (
          <div
            key={zone.zoneId}
            className="inline-flex items-center gap-2 px-2.5 py-1 rounded-md bg-slate-50 border border-slate-200 text-xs"
          >
            <span
              className="w-3 h-3 rounded-full border border-white shadow-sm"
              style={{ backgroundColor: zone.zoneColor }}
              aria-hidden="true"
            />
            <span className="font-medium text-slate-700">{zone.zoneName}</span>
            <span className="text-slate-400">
              {zone.availableCount}/{zone.totalCount}
            </span>
          </div>
        ))}
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Seat status legend                                                 */}
      {/* ------------------------------------------------------------------ */}
      <div className="flex flex-wrap gap-4 text-xs text-slate-600">
        <LegendItem color="bg-emerald-100 border-emerald-400" label="Available" />
        <LegendItem color="bg-blue-500 border-blue-300 ring-2 ring-blue-400/50" label="Your Selection" />
        <LegendItem color="bg-amber-300 border-amber-400" label="Held by Others" />
        <LegendItem color="bg-slate-400 border-slate-500" label="Reserved" />
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Seat map grid, grouped by zone                                     */}
      {/* ------------------------------------------------------------------ */}
      <div className="flex flex-col gap-6 p-4 bg-slate-50 rounded-lg border border-slate-200 overflow-x-auto">
        {/* Stage indicator */}
        <div className="flex justify-center">
          <div className="px-8 py-1.5 bg-slate-200 rounded-b-xl text-xs font-medium text-slate-500 uppercase tracking-wider">
            Stage
          </div>
        </div>

        {zones.map((zone) => (
          <div key={zone.zoneId} className="flex flex-col gap-2">
            {/* Zone header */}
            <div className="flex items-center gap-2 mb-1">
              <span
                className="w-2.5 h-2.5 rounded-full"
                style={{ backgroundColor: zone.zoneColor }}
                aria-hidden="true"
              />
              <span className="text-xs font-semibold text-slate-600 uppercase tracking-wide">
                {zone.zoneName}
              </span>
            </div>

            {/* Rows */}
            {zone.rows.map(({ rowLabel, seats }) => (
              <div key={`${zone.zoneId}-${rowLabel}`} className="flex items-center gap-1.5">
                {/* Row label */}
                <span className="w-6 text-right text-[11px] font-medium text-slate-400 shrink-0">
                  {rowLabel}
                </span>

                {/* Seats */}
                <div className="flex items-center gap-1 flex-wrap">
                  {seats.map((seat) => (
                    <SeatCircle
                      key={seat.id}
                      seat={seat}
                      isSelected={selectedSeatIds.has(seat.id)}
                      canSelect={selectedSeatIds.size < maxSeats}
                      onClick={handleSeatClick}
                    />
                  ))}
                </div>
              </div>
            ))}
          </div>
        ))}
      </div>

      {/* ------------------------------------------------------------------ */}
      {/* Selection summary                                                  */}
      {/* ------------------------------------------------------------------ */}
      {selectedSeatLabels.length > 0 && (
        <div className="flex flex-col gap-1.5 p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <div className="flex items-center gap-2">
            <Info className="w-4 h-4 text-blue-500" />
            <span className="text-sm font-medium text-blue-800">
              Selected Seats ({selectedSeatLabels.length})
            </span>
          </div>
          <div className="flex flex-wrap gap-1.5 ml-6">
            {selectedSeatLabels.map((label) => (
              <span
                key={label}
                className="inline-flex items-center px-2 py-0.5 bg-blue-100 text-blue-700 text-xs font-medium rounded"
              >
                {label}
              </span>
            ))}
          </div>
        </div>
      )}

      {/* ------------------------------------------------------------------ */}
      {/* Action bar                                                         */}
      {/* ------------------------------------------------------------------ */}
      <div className="flex items-center justify-end gap-3 pt-2">
        <Button
          type="button"
          variant="outline"
          onClick={handleCancel}
        >
          <X className="w-4 h-4 mr-1.5" />
          Cancel
        </Button>

        <Button
          type="button"
          disabled={selectedSeatIds.size !== maxSeats || holdSeatsMutation.isPending}
          loading={holdSeatsMutation.isPending}
          onClick={handleConfirm}
        >
          <Check className="w-4 h-4 mr-1.5" />
          Confirm Seats
        </Button>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Legend item sub-component
// ---------------------------------------------------------------------------

function LegendItem({ color, label }: { color: string; label: string }) {
  return (
    <div className="inline-flex items-center gap-1.5">
      <span className={`w-4 h-4 rounded-full border ${color}`} aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}
