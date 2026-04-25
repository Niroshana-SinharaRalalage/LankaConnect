'use client';

import { useState } from 'react';
import { Armchair, Info, Loader2 } from 'lucide-react';
import { SeatingMode, TicketingMode } from '@/infrastructure/api/types/events.types';
import { SeatingLayoutPicker } from './SeatingLayoutPicker';
import type { VenueLayoutDto } from '@/infrastructure/api/types/events.types';

/**
 * Seating Redesign Slice 1: Inline seating configuration section for event
 * creation and edit forms.
 *
 * Rules:
 *  - Only rendered when `ticketingMode === Tiered`. Assigned seating requires
 *    tiered ticketing server-side; we gate the UI so the choice is never
 *    offered for single-tier events.
 *  - Slice 1 scope is UI-shell only: the toggle records user intent, but
 *    layout creation (zones, tables, seats) is deferred to Slice 2+3.
 *  - Pure controlled component: no fetching here. Parent forms persist the
 *    value via `useSetSeatingMode` (edit) or after event create (create).
 */

export interface SeatingSectionProps {
  /** Current ticketing mode. SeatingSection returns null unless this is Tiered. */
  ticketingMode: TicketingMode;
  /** Current seating mode value. */
  value: SeatingMode;
  /** Called with the new seating mode when the user toggles. */
  onChange: (mode: SeatingMode) => void;
  /** Optional async flag used to show a spinner on the toggle while persisting. */
  isSaving?: boolean;
  /** Optional error message rendered below the toggle (e.g. server 400). */
  errorMessage?: string | null;
  /** Disable the toggle entirely (e.g. event already has registrations). */
  disabled?: boolean;
  /** Reason surfaced to the user when `disabled` is true. */
  disabledReason?: string;
  /**
   * Slice 6 S6.9: when supplied, AssignedSeating mode exposes the layout
   * picker (preset modal + preview). For the event CREATION flow the event
   * does not exist yet — omit this prop and the old "save first" placeholder
   * is shown instead.
   */
  eventId?: string;
  /** Optional hook for the parent to react to a preset being applied. */
  onLayoutChanged?: (layout: VenueLayoutDto) => void;
}

export function SeatingSection({
  ticketingMode,
  value,
  onChange,
  isSaving = false,
  errorMessage = null,
  disabled = false,
  disabledReason,
  eventId,
  onLayoutChanged,
}: SeatingSectionProps) {
  const [hasInteracted, setHasInteracted] = useState(false);

  // Gate: only Tiered events can use assigned seating.
  if (ticketingMode !== TicketingMode.Tiered) {
    return null;
  }

  const isAssigned = value === SeatingMode.AssignedSeating;

  const handleToggle = () => {
    if (disabled || isSaving) return;
    setHasInteracted(true);
    const next = isAssigned ? SeatingMode.GeneralAdmission : SeatingMode.AssignedSeating;
    onChange(next);
  };

  return (
    <div
      data-testid="seating-section"
      className="rounded-lg border border-neutral-200 bg-white p-4 space-y-4"
    >
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 p-2 bg-primary-50 rounded-md">
            <Armchair className="w-5 h-5 text-primary-600" aria-hidden="true" />
          </div>
          <div>
            <h3 className="text-base font-semibold text-neutral-900">
              Assigned Seating
            </h3>
            <p className="text-sm text-neutral-600 mt-1">
              Give attendees a specific seat at the venue. Each ticket tier maps to
              a seating zone you configure in the venue layout.
            </p>
          </div>
        </div>

        {/* Toggle */}
        <label
          className={`relative inline-flex items-center cursor-pointer shrink-0 ${
            disabled || isSaving ? 'opacity-60 cursor-not-allowed' : ''
          }`}
          aria-label="Enable assigned seating"
        >
          <input
            type="checkbox"
            role="switch"
            aria-checked={isAssigned}
            checked={isAssigned}
            onChange={handleToggle}
            disabled={disabled || isSaving}
            className="sr-only peer"
            data-testid="seating-toggle"
          />
          <div
            className="w-11 h-6 bg-neutral-200 peer-focus:ring-2 peer-focus:ring-primary-300 rounded-full
                       peer peer-checked:bg-primary-600
                       after:content-[''] after:absolute after:top-[2px] after:left-[2px]
                       after:bg-white after:rounded-full after:h-5 after:w-5 after:transition-all
                       peer-checked:after:translate-x-full peer-checked:after:border-white"
          />
          {isSaving && (
            <Loader2
              className="ml-2 w-4 h-4 animate-spin text-primary-600"
              data-testid="seating-saving"
              aria-hidden="true"
            />
          )}
        </label>
      </div>

      {disabled && disabledReason && (
        <p
          className="text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-md px-3 py-2"
          role="note"
        >
          {disabledReason}
        </p>
      )}

      {errorMessage && (
        <p
          className="text-sm text-red-700 bg-red-50 border border-red-200 rounded-md px-3 py-2"
          role="alert"
          data-testid="seating-error"
        >
          {errorMessage}
        </p>
      )}

      {/* Slice 6 S6.9: Event-aware layout picker (preset modal + live preview).
          Rendered only once the event exists (edit flow); the create flow
          shows the "save first" hint below until the event is persisted. */}
      {isAssigned && eventId && (
        <SeatingLayoutPicker eventId={eventId} onLayoutChanged={onLayoutChanged} />
      )}

      {isAssigned && !eventId && (
        <div
          data-testid="seating-layout-placeholder"
          className="rounded-md border border-dashed border-primary-200 bg-primary-50/30 px-4 py-3 flex items-start gap-3"
        >
          <Info className="w-5 h-5 text-primary-600 mt-0.5 shrink-0" aria-hidden="true" />
          <div className="text-sm text-neutral-700">
            <p className="font-medium text-neutral-900">
              Save the event first, then pick a seating layout
            </p>
            <p className="mt-1">
              Assigned seating is enabled. Once you save, you can open the preset
              library and choose a starting layout (you&apos;ll be able to customize it).
            </p>
          </div>
        </div>
      )}

      {hasInteracted && !isAssigned && (
        <p className="text-xs text-neutral-500">
          Assigned seating disabled. Attendees will not pick a seat at registration.
        </p>
      )}
    </div>
  );
}
