'use client';

import React from 'react';
import { Info } from 'lucide-react';
import { RegistrationMode } from '@/infrastructure/api/types/events.types';

export type RegistrationStatusHintVariant = 'banner' | 'pill';

export interface RegistrationStatusHintProps {
  registrationMode: RegistrationMode;
  variant: RegistrationStatusHintVariant;
  /**
   * When the event is cancelled, the cancelled banner / "Event Cancelled" displayLabel
   * pill take precedence and we don't want a competing "No registration required" hint.
   */
  isCancelled?: boolean;
}

/**
 * Phase 8YB.3 — public-facing hint surface for registration-mode metadata that
 * doesn't fit cleanly into the registration form section.
 *
 * Today this only renders for `NoRegistration` (Mode C, drop-in events): the user
 * reported that the existing "No registration required" copy in `RsvpFormSection`
 * was buried inside a collapsed section below the fold, and the quick-nav pill row
 * was actively *removing* the "Register" anchor without a replacement. This component
 * is consumed twice from `events/[id]/page.tsx`:
 *
 *   - `variant="banner"` — full blue Info card under the title + badges row, above
 *                          the RTE description, so the message lands above the fold.
 *   - `variant="pill"`   — compact status pill in the quick-nav row, replacing the
 *                          hidden Register anchor so the row doesn't silently shrink.
 *
 * Other modes (DetailedAttendees, HeadCount*, External) render nothing — they have
 * their own surfaces (RsvpFormSection bodies, ExternalRegistrationCta). When the
 * event is cancelled, this component also renders nothing so the cancelled banner /
 * Cancelled displayLabel pill remain the dominant signal.
 *
 * Adding a hint for a future mode = one extra branch in this component, no edits to
 * `page.tsx`, `RsvpFormSection`, or the quick-nav row.
 */
export const RegistrationStatusHint: React.FC<RegistrationStatusHintProps> = ({
  registrationMode,
  variant,
  isCancelled = false,
}) => {
  if (isCancelled) return null;
  if (registrationMode !== RegistrationMode.NoRegistration) return null;

  if (variant === 'pill') {
    return (
      <span
        className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-md border border-blue-300 bg-blue-50 text-blue-800"
        aria-label="No registration required for this event"
      >
        <Info className="h-3.5 w-3.5" aria-hidden="true" />
        No registration required
      </span>
    );
  }

  return (
    <div
      className="rounded-lg border border-blue-200 bg-blue-50 p-4"
      role="note"
      aria-label="Registration status"
    >
      <div className="flex items-start gap-2">
        <Info className="h-5 w-5 text-blue-600 mt-0.5 flex-shrink-0" aria-hidden="true" />
        <div>
          <p className="text-sm font-semibold text-blue-900">
            No registration required for this event
          </p>
          <p className="text-sm text-blue-800 mt-1">
            This is a drop-in event — just show up. Any sign-up lists, signup forms,
            donations, sponsorships, collections or add-ons the organizer has set up
            remain available via the actions on this page.
          </p>
        </div>
      </div>
    </div>
  );
};

export default RegistrationStatusHint;
